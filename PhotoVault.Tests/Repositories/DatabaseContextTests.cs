using Dapper;

namespace PhotoVault.Tests.Repositories;

public class DatabaseContextTests
{
    [Fact]
    public void Initialize_CreatesAllTablesAndIndexes()
    {
        using var db = new TestDatabase();
        using var connection = db.Context.OpenConnection();

        var tables = connection.Query<string>("SELECT name FROM sqlite_master WHERE type = 'table'").ToHashSet();
        Assert.Superset(new HashSet<string> { "SourceFolders", "Photos", "Albums", "AlbumPhotos", "Tags", "PhotoTags", "AppSettings" }, tables);

        var indexes = connection.Query<string>("SELECT name FROM sqlite_master WHERE type = 'index'").ToHashSet();
        Assert.Contains("idx_photos_filename", indexes);
        Assert.Contains("idx_photos_sourcefolder", indexes);

        Assert.Equal(2, db.Context.GetSchemaVersion());
    }

    [Fact]
    public void Initialize_IsIdempotent()
    {
        using var db = new TestDatabase();
        db.Context.Initialize();
        db.Context.Initialize();
        Assert.Equal(2, db.Context.GetSchemaVersion());
    }

    [Fact]
    public void ForeignKeys_CascadeDeletesFromSourceFolderToPhotosAndLinks()
    {
        using var db = new TestDatabase();
        using var c = db.Context.OpenConnection();

        c.Execute("INSERT INTO SourceFolders (FolderPath, DateAdded) VALUES ('C:\\Poze', '2026-01-01')");
        c.Execute("INSERT INTO Photos (SourceFolderId, FullPath, FileName, Extension, DateAdded) VALUES (1, 'C:\\Poze\\a.jpg', 'a.jpg', 'jpg', '2026-01-01')");
        c.Execute("INSERT INTO Albums (Name, DateCreated, CoverPhotoId) VALUES ('Vacanță', '2026-01-01', 1)");
        c.Execute("INSERT INTO AlbumPhotos (AlbumId, PhotoId, DateAdded) VALUES (1, 1, '2026-01-01')");
        c.Execute("INSERT INTO Tags (Name) VALUES ('mare')");
        c.Execute("INSERT INTO PhotoTags (PhotoId, TagId) VALUES (1, 1)");

        c.Execute("DELETE FROM SourceFolders WHERE Id = 1");

        Assert.Equal(0, c.ExecuteScalar<long>("SELECT COUNT(*) FROM Photos"));
        Assert.Equal(0, c.ExecuteScalar<long>("SELECT COUNT(*) FROM AlbumPhotos"));
        Assert.Equal(0, c.ExecuteScalar<long>("SELECT COUNT(*) FROM PhotoTags"));
        // Albumul și tag-ul rămân; coperta devine NULL
        Assert.Equal(1, c.ExecuteScalar<long>("SELECT COUNT(*) FROM Albums"));
        Assert.Null(c.ExecuteScalar<long?>("SELECT CoverPhotoId FROM Albums WHERE Id = 1"));
        Assert.Equal(1, c.ExecuteScalar<long>("SELECT COUNT(*) FROM Tags"));
    }
}
