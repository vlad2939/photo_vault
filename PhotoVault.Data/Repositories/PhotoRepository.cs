using Dapper;
using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;

namespace PhotoVault.Data.Repositories;

public sealed class PhotoRepository(DatabaseContext db) : IPhotoRepository
{
    private const string SelectColumns =
        """
        SELECT Id, SourceFolderId, FullPath, FileName, Extension, FileSizeBytes, DateAdded,
               DateTakenExif, RotationDegrees, ThumbnailPath, IsMissing
        FROM Photos
        """;

    public IReadOnlyList<PhotoItem> GetAll()
    {
        using var connection = db.OpenConnection();
        return connection.Query<Row>($"{SelectColumns} ORDER BY FileName COLLATE NOCASE, FullPath COLLATE NOCASE")
            .Select(Map).ToList();
    }

    public int Count()
    {
        using var connection = db.OpenConnection();
        return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Photos");
    }

    public IReadOnlyDictionary<long, int> CountBySourceFolder()
    {
        using var connection = db.OpenConnection();
        return connection.Query<(long FolderId, int Count)>(
                "SELECT SourceFolderId, COUNT(*) FROM Photos GROUP BY SourceFolderId")
            .ToDictionary(r => r.FolderId, r => r.Count);
    }

    public IReadOnlyList<PhotoItem> GetBySourceFolder(long sourceFolderId)
    {
        using var connection = db.OpenConnection();
        return connection.Query<Row>($"{SelectColumns} WHERE SourceFolderId = @sourceFolderId", new { sourceFolderId })
            .Select(Map).ToList();
    }

    public IReadOnlyList<PhotoItem> GetWithoutThumbnail()
    {
        using var connection = db.OpenConnection();
        // Ordinea din grid → miniaturile apar de sus în jos
        return connection.Query<Row>($"{SelectColumns} WHERE ThumbnailPath IS NULL ORDER BY FileName COLLATE NOCASE")
            .Select(Map).ToList();
    }

    public void InsertMany(IReadOnlyList<PhotoItem> photos)
    {
        if (photos.Count == 0) return;
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO Photos (SourceFolderId, FullPath, FileName, Extension, FileSizeBytes, DateAdded, RotationDegrees, IsMissing)
            VALUES ($folder, $path, $name, $ext, $size, $added, 0, 0);
            SELECT last_insert_rowid();
            """;
        var folder = command.Parameters.Add("$folder", Microsoft.Data.Sqlite.SqliteType.Integer);
        var path = command.Parameters.Add("$path", Microsoft.Data.Sqlite.SqliteType.Text);
        var name = command.Parameters.Add("$name", Microsoft.Data.Sqlite.SqliteType.Text);
        var ext = command.Parameters.Add("$ext", Microsoft.Data.Sqlite.SqliteType.Text);
        var size = command.Parameters.Add("$size", Microsoft.Data.Sqlite.SqliteType.Integer);
        var added = command.Parameters.Add("$added", Microsoft.Data.Sqlite.SqliteType.Text);

        // Comandă pregătită o singură dată, refolosită pentru tot lotul (rapid la zeci de mii de rânduri)
        foreach (var photo in photos)
        {
            folder.Value = photo.SourceFolderId;
            path.Value = photo.FullPath;
            name.Value = photo.FileName;
            ext.Value = photo.Extension;
            size.Value = (object?)photo.FileSizeBytes ?? DBNull.Value;
            added.Value = SqliteDates.ToDb(photo.DateAdded);
            photo.Id = (long)command.ExecuteScalar()!;
        }
        transaction.Commit();
    }

    public void DeleteMany(IReadOnlyCollection<long> ids)
    {
        if (ids.Count == 0) return;
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();
        foreach (var chunk in ids.Chunk(500))
            connection.Execute("DELETE FROM Photos WHERE Id IN @chunk", new { chunk }, transaction);
        transaction.Commit();
    }

    public void UpdateThumbnails(IReadOnlyCollection<ThumbnailResult> results)
    {
        if (results.Count == 0) return;
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();
        // ThumbnailPath '' = imagine ilizibilă → nu se mai reîncearcă la fiecare pornire
        connection.Execute(
            "UPDATE Photos SET ThumbnailPath = @Path, DateTakenExif = COALESCE(@Taken, DateTakenExif) WHERE Id = @Id",
            results.Select(r => new { Id = r.PhotoId, Path = r.ThumbnailPath ?? string.Empty, Taken = SqliteDates.ToDb(r.DateTaken) }),
            transaction);
        transaction.Commit();
    }

    public IReadOnlyDictionary<long, int> GetRotations(IReadOnlyCollection<long> photoIds)
    {
        using var connection = db.OpenConnection();
        var result = new Dictionary<long, int>();
        foreach (var chunk in photoIds.Chunk(500))
            foreach (var (id, rotation) in connection.Query<(long, long)>(
                         "SELECT Id, RotationDegrees FROM Photos WHERE Id IN @chunk", new { chunk }))
                result[id] = (int)rotation;
        return result;
    }

    public void SetRotations(IReadOnlyDictionary<long, int> rotations)
    {
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();
        connection.Execute("UPDATE Photos SET RotationDegrees = @Value WHERE Id = @Key", rotations, transaction);
        transaction.Commit();
    }

    public IReadOnlyDictionary<long, string> GetSearchKeywords()
    {
        using var connection = db.OpenConnection();
        return connection.Query<(long Id, string? Tags, string? Albums)>(
                """
                SELECT p.Id,
                       (SELECT group_concat(t.Name, ' ') FROM PhotoTags pt JOIN Tags t ON t.Id = pt.TagId WHERE pt.PhotoId = p.Id),
                       (SELECT group_concat(a.Name, ' ') FROM AlbumPhotos ap JOIN Albums a ON a.Id = ap.AlbumId WHERE ap.PhotoId = p.Id)
                FROM Photos p
                WHERE EXISTS (SELECT 1 FROM PhotoTags pt WHERE pt.PhotoId = p.Id)
                   OR EXISTS (SELECT 1 FROM AlbumPhotos ap WHERE ap.PhotoId = p.Id)
                """)
            .ToDictionary(r => r.Id, r => $"{r.Tags} {r.Albums}".Trim());
    }

    public void ResetFailedThumbnails(long sourceFolderId)
    {
        using var connection = db.OpenConnection();
        connection.Execute("UPDATE Photos SET ThumbnailPath = NULL WHERE SourceFolderId = @sourceFolderId AND ThumbnailPath = ''",
            new { sourceFolderId });
    }

    public int CountInFolder(string folderPath)
    {
        var prefix = Path.TrimEndingDirectorySeparator(folderPath) + Path.DirectorySeparatorChar;
        using var connection = db.OpenConnection();
        var paths = connection.Query<string>("SELECT FullPath FROM Photos WHERE FullPath LIKE @pattern ESCAPE '^'",
            new { pattern = EscapeLike(prefix) + "%" });
        // Doar fișierele aflate direct în folder, nu în subfoldere
        return paths.Count(p => p.Length > prefix.Length && p.IndexOf(Path.DirectorySeparatorChar, prefix.Length) < 0);
    }

    public int UpdatePaths(IReadOnlyList<(string OldPath, string NewPath)> renames)
    {
        if (renames.Count == 0) return 0;
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();
        // Două etape, ca pe disc: FullPath e UNIQUE, iar un schimb de nume (A→B, B→A) ar încălca temporar constrângerea
        const string marker = "|";   // caracter imposibil într-o cale Windows
        foreach (var (oldPath, _) in renames)
            connection.Execute("UPDATE Photos SET FullPath = @temp WHERE FullPath = @oldPath COLLATE NOCASE",
                new { temp = marker + oldPath, oldPath }, transaction);
        var updated = 0;
        foreach (var (oldPath, newPath) in renames)
            updated += connection.Execute("UPDATE Photos SET FullPath = @newPath, FileName = @name WHERE FullPath = @temp",
                new { newPath, name = Path.GetFileName(newPath), temp = marker + oldPath }, transaction);
        transaction.Commit();
        return updated;
    }

    private static string EscapeLike(string value) =>
        value.Replace("^", "^^").Replace("%", "^%").Replace("_", "^_");

    private static PhotoItem Map(Row r) => new()
    {
        Id = r.Id,
        SourceFolderId = r.SourceFolderId,
        FullPath = r.FullPath,
        FileName = r.FileName,
        Extension = r.Extension,
        FileSizeBytes = r.FileSizeBytes,
        DateAdded = SqliteDates.FromDb(r.DateAdded),
        DateTakenExif = SqliteDates.FromDbNullable(r.DateTakenExif),
        RotationDegrees = (int)r.RotationDegrees,
        ThumbnailPath = r.ThumbnailPath,
        IsMissing = r.IsMissing != 0,
    };

    private sealed record Row(long Id, long SourceFolderId, string FullPath, string FileName, string Extension,
        long? FileSizeBytes, string DateAdded, string? DateTakenExif, long RotationDegrees, string? ThumbnailPath,
        long IsMissing);
}
