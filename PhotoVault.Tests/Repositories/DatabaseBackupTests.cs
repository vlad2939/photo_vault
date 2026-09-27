using Dapper;
using Microsoft.Data.Sqlite;
using PhotoVault.Core.Services;
using PhotoVault.Data;
using PhotoVault.Data.Repositories;

namespace PhotoVault.Tests.Repositories;

public sealed class DatabaseBackupTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private static long CountAlbums(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        return connection.ExecuteScalar<long>("SELECT COUNT(*) FROM Albums");
    }

    [Fact]
    public void CreateBackup_IsConsistentCopy_NextToDatabase()
    {
        var albums = new AlbumService(new AlbumRepository(_db.Context));
        albums.Create("Vacanță");
        var backup = new DatabaseBackup(_db.Context);

        var info = backup.CreateBackup();

        Assert.NotNull(info);
        Assert.Equal(Path.GetDirectoryName(_db.Context.DatabasePath), Path.GetDirectoryName(info.Path));
        Assert.StartsWith("photovault.db.bak.", Path.GetFileName(info.Path));
        Assert.Equal(1, CountAlbums(info.Path));   // include și ce era doar în jurnalul WAL
        Assert.Single(backup.GetBackups());
    }

    [Fact]
    public void CreateBackup_ThrottledWithinInterval_UnlessForced()
    {
        var backup = new DatabaseBackup(_db.Context, minInterval: TimeSpan.FromMinutes(5));

        Assert.NotNull(backup.CreateBackup());
        Assert.Null(backup.CreateBackup());          // operațiuni în lanț → copia de dinainte rămâne relevantă
        Assert.NotNull(backup.CreateBackup(force: true));
        Assert.Equal(2, backup.GetBackups().Count);
    }

    [Fact]
    public void Rotation_KeepsOnlyNewestCopies()
    {
        var backup = new DatabaseBackup(_db.Context, maxBackups: 3, minInterval: TimeSpan.Zero);
        for (var i = 0; i < 6; i++) Assert.NotNull(backup.CreateBackup());

        var backups = backup.GetBackups();
        Assert.Equal(3, backups.Count);
        Assert.True(backups[0].Created >= backups[^1].Created);
    }

    [Fact]
    public void DestructiveOperations_CreateBackupBeforeChanging()
    {
        var backup = new DatabaseBackup(_db.Context, minInterval: TimeSpan.Zero);
        var albums = new AlbumService(new AlbumRepository(_db.Context), backup);
        var tags = new TagService(new TagRepository(_db.Context), backup);
        var album = albums.Create("De șters");
        var tag = tags.GetOrCreate("mare");

        albums.Delete(album.Id);
        var afterAlbum = backup.GetBackups();
        Assert.Single(afterAlbum);
        Assert.Equal(1, CountAlbums(afterAlbum[0].Path));   // copia conține albumul dinaintea ștergerii

        tags.Delete(tag.Id);
        Assert.Equal(2, backup.GetBackups().Count);
    }
}
