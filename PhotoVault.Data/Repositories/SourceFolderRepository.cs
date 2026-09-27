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

    private static SourceFolder Map(Row r) => new()
    {
        Id = r.Id,
        FolderPath = r.FolderPath,
        DateAdded = SqliteDates.FromDb(r.DateAdded),
        LastScanned = SqliteDates.FromDbNullable(r.LastScanned),
    };

    private sealed record Row(long Id, string FolderPath, string DateAdded, string? LastScanned);
}
