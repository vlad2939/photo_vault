using Dapper;
using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;

namespace PhotoVault.Data.Repositories;

public sealed class SourceFolderRepository(DatabaseContext db) : ISourceFolderRepository
{
    private const string SelectColumns = "SELECT Id, FolderPath, DateAdded, LastScanned FROM SourceFolders";

    public IReadOnlyList<SourceFolder> GetAll()
    {
        using var connection = db.OpenConnection();
        return connection.Query<Row>($"{SelectColumns} ORDER BY FolderPath COLLATE NOCASE").Select(Map).ToList();
    }

    public SourceFolder? GetById(long id)
    {
        using var connection = db.OpenConnection();
        var row = connection.QuerySingleOrDefault<Row>($"{SelectColumns} WHERE Id = @id", new { id });
        return row is null ? null : Map(row);
    }

    public SourceFolder Add(string folderPath)
    {
        var dateAdded = DateTime.Now;
        using var connection = db.OpenConnection();
        var id = connection.ExecuteScalar<long>(
            "INSERT INTO SourceFolders (FolderPath, DateAdded) VALUES (@folderPath, @dateAdded); SELECT last_insert_rowid();",
            new { folderPath, dateAdded = SqliteDates.ToDb(dateAdded) });
        return new SourceFolder { Id = id, FolderPath = folderPath, DateAdded = SqliteDates.FromDb(SqliteDates.ToDb(dateAdded)) };
    }

    public void Delete(long id)
    {
        using var connection = db.OpenConnection();
        connection.Execute("DELETE FROM SourceFolders WHERE Id = @id", new { id });
    }

    public void UpdateLastScanned(long id, DateTime lastScanned)
    {
        using var connection = db.OpenConnection();
        connection.Execute("UPDATE SourceFolders SET LastScanned = @value WHERE Id = @id",
            new { id, value = SqliteDates.ToDb(lastScanned) });
    }

    public void Relocate(long id, string newFolderPath)
    {
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var oldFolderPath = connection.ExecuteScalar<string>("SELECT FolderPath FROM SourceFolders WHERE Id = @id", new { id }, transaction)
                            ?? throw new InvalidOperationException($"Folder sursă inexistent: {id}");
        var oldRoot = WithSeparator(oldFolderPath);
        var newRoot = WithSeparator(newFolderPath);

        connection.Execute("UPDATE SourceFolders SET FolderPath = @newFolderPath WHERE Id = @id", new { id, newFolderPath }, transaction);

        // Două etape (marcaj temporar, apoi calea finală): FullPath e UNIQUE, iar noua locație poate fi
        // chiar un subfolder al celei vechi — o actualizare directă ar putea ciocni temporar două rânduri.
        const string marker = "|";
        connection.Execute(
            """
            UPDATE Photos SET FullPath = @marker || @newRoot || substr(FullPath, length(@oldRoot) + 1)
            WHERE SourceFolderId = @id AND substr(FullPath, 1, length(@oldRoot)) = @oldRoot COLLATE NOCASE
            """, new { id, marker, oldRoot, newRoot }, transaction);
        connection.Execute(
            "UPDATE Photos SET FullPath = substr(FullPath, 2) WHERE SourceFolderId = @id AND substr(FullPath, 1, 1) = @marker",
            new { id, marker }, transaction);
        transaction.Commit();
    }

    private static string WithSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;

    private static SourceFolder Map(Row r) => new()
    {
        Id = r.Id,
        FolderPath = r.FolderPath,
        DateAdded = SqliteDates.FromDb(r.DateAdded),
        LastScanned = SqliteDates.FromDbNullable(r.LastScanned),
    };

    private sealed record Row(long Id, string FolderPath, string DateAdded, string? LastScanned);
}
