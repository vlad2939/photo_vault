using Dapper;
using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;

namespace PhotoVault.Data.Repositories;

public sealed class AlbumRepository(DatabaseContext db) : IAlbumRepository
{
    public IReadOnlyList<AlbumSummary> GetAll()
    {
        using var connection = db.OpenConnection();
        // Coperta efectivă: cea setată manual, altfel prima poză adăugată în album (§5.7)
        return connection.Query<Row>(
                """
                SELECT x.Id, x.Name, x.DateCreated, x.PhotoCount, x.EffectiveCover, p.ThumbnailPath
                FROM (
                    SELECT a.Id, a.Name, a.DateCreated,
                           (SELECT COUNT(*) FROM AlbumPhotos ap WHERE ap.AlbumId = a.Id) AS PhotoCount,
                           COALESCE(a.CoverPhotoId,
                                    (SELECT ap.PhotoId FROM AlbumPhotos ap WHERE ap.AlbumId = a.Id
                                     ORDER BY ap.SortOrder, ap.DateAdded, ap.PhotoId LIMIT 1)) AS EffectiveCover
                    FROM Albums a
                ) x
                LEFT JOIN Photos p ON p.Id = x.EffectiveCover
                ORDER BY x.Name COLLATE NOCASE, x.Id
                """)
            .Select(r => new AlbumSummary(
                new Album { Id = r.Id, Name = r.Name, DateCreated = SqliteDates.FromDb(r.DateCreated), CoverPhotoId = r.EffectiveCover },
                (int)r.PhotoCount, r.EffectiveCover, r.ThumbnailPath))
            .ToList();
    }

    public Album Create(string name)
    {
        var created = SqliteDates.FromDb(SqliteDates.ToDb(DateTime.Now));
        using var connection = db.OpenConnection();
        var id = connection.ExecuteScalar<long>(
            "INSERT INTO Albums (Name, DateCreated) VALUES (@name, @created); SELECT last_insert_rowid();",
            new { name, created = SqliteDates.ToDb(created) });
        return new Album { Id = id, Name = name, DateCreated = created };
    }

    public void Rename(long albumId, string name)
    {
        using var connection = db.OpenConnection();
        connection.Execute("UPDATE Albums SET Name = @name WHERE Id = @albumId", new { albumId, name });
    }

    public void Delete(long albumId)
    {
        using var connection = db.OpenConnection();
        connection.Execute("DELETE FROM Albums WHERE Id = @albumId", new { albumId });   // cascadă → AlbumPhotos
    }

    public int AddPhotos(long albumId, IReadOnlyCollection<long> photoIds)
    {
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var nextOrder = connection.ExecuteScalar<long>(
            "SELECT COALESCE(MAX(SortOrder), -1) + 1 FROM AlbumPhotos WHERE AlbumId = @albumId", new { albumId }, transaction);
        var now = SqliteDates.ToDb(DateTime.Now);
        var added = 0;
        foreach (var photoId in photoIds)
        {
            added += connection.Execute(
                "INSERT OR IGNORE INTO AlbumPhotos (AlbumId, PhotoId, DateAdded, SortOrder) VALUES (@albumId, @photoId, @now, @order)",
                new { albumId, photoId, now, order = nextOrder++ }, transaction);
        }
        transaction.Commit();
        return added;
    }

    public int RemovePhotos(long albumId, IReadOnlyCollection<long> photoIds)
    {
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var removed = 0;
        foreach (var chunk in photoIds.Chunk(500))
            removed += connection.Execute("DELETE FROM AlbumPhotos WHERE AlbumId = @albumId AND PhotoId IN @chunk",
                new { albumId, chunk }, transaction);
        // Coperta manuală eliminată din album → revine la coperta implicită
        connection.Execute(
            """
            UPDATE Albums SET CoverPhotoId = NULL
            WHERE Id = @albumId AND CoverPhotoId IS NOT NULL
              AND CoverPhotoId NOT IN (SELECT PhotoId FROM AlbumPhotos WHERE AlbumId = @albumId)
            """, new { albumId }, transaction);
        transaction.Commit();
        return removed;
    }

    public IReadOnlySet<long> GetPhotoIds(long albumId)
    {
        using var connection = db.OpenConnection();
        return connection.Query<long>("SELECT PhotoId FROM AlbumPhotos WHERE AlbumId = @albumId", new { albumId }).ToHashSet();
    }

    public void SetCover(long albumId, long? photoId)
    {
        using var connection = db.OpenConnection();
        connection.Execute("UPDATE Albums SET CoverPhotoId = @photoId WHERE Id = @albumId", new { albumId, photoId });
    }

    /// <summary>
    /// Clasă cu proprietăți (nu record pozițional): coloanele calculate nu au tip declarat în SQLite,
    /// iar Dapper le convertește corect doar la maparea pe proprietăți.
    /// </summary>
    private sealed class Row
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string DateCreated { get; init; } = string.Empty;
        public long PhotoCount { get; init; }
        public long? EffectiveCover { get; init; }
        public string? ThumbnailPath { get; init; }
    }
}
