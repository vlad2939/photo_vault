using Dapper;
using PhotoVault.Core.Services;
using PhotoVault.Data.Repositories;

namespace PhotoVault.Tests.Services;

public sealed class AlbumAndTagServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly AlbumService _albums;
    private readonly TagService _tags;
    private readonly long[] _photoIds;

    public AlbumAndTagServiceTests()
    {
        _albums = new AlbumService(new AlbumRepository(_db.Context));
        _tags = new TagService(new TagRepository(_db.Context));

        using var c = _db.Context.OpenConnection();
        c.Execute("INSERT INTO SourceFolders (FolderPath, DateAdded) VALUES ('C:\\Poze', '2026-01-01T00:00:00')");
        foreach (var name in new[] { "a.jpg", "b.jpg", "c.jpg" })
            c.Execute("INSERT INTO Photos (SourceFolderId, FullPath, FileName, Extension, DateAdded, ThumbnailPath) VALUES (1, @path, @name, 'jpg', '2026-01-01T00:00:00', @thumb)",
                new { path = "C:\\Poze\\" + name, name, thumb = "aa/" + name });
        _photoIds = c.Query<long>("SELECT Id FROM Photos ORDER BY FileName").ToArray();
    }

    [Fact]
    public void Album_AddRemovePhotos_CountsAndCoverFollowFirstAdded()
    {
        var album = _albums.Create("  Vacanță Grecia 2024 ");
        Assert.Equal("Vacanță Grecia 2024", album.Name);

        Assert.Equal(2, _albums.AddPhotos(album.Id, [_photoIds[1], _photoIds[0]]));
        Assert.Equal(1, _albums.AddPhotos(album.Id, [_photoIds[0], _photoIds[2]]));   // a.jpg era deja în album

        var summary = _albums.GetAlbums().Single();
        Assert.Equal(3, summary.PhotoCount);
        Assert.Equal(_photoIds[1], summary.CoverPhotoId);            // prima poză adăugată
        Assert.Equal("aa/b.jpg", summary.CoverThumbnailPath);

        _albums.SetCover(album.Id, _photoIds[2]);
        Assert.Equal(_photoIds[2], _albums.GetAlbums().Single().CoverPhotoId);

        // Eliminarea copertei manuale din album → revine la coperta implicită
        Assert.Equal(1, _albums.RemovePhotos(album.Id, [_photoIds[2]]));
        Assert.Equal(_photoIds[1], _albums.GetAlbums().Single().CoverPhotoId);
        Assert.Equal(new HashSet<long> { _photoIds[0], _photoIds[1] }, _albums.GetPhotoIds(album.Id));
    }

    [Fact]
    public void Album_RenameAndDelete_KeepPhotosIndexed()
    {
        var album = _albums.Create("Temp");
        _albums.AddPhotos(album.Id, _photoIds);
        _albums.Update(album.Id, "Final", "  10–15.08.2021 ");
        Assert.Equal("Final", _albums.GetAlbums().Single().Album.Name);
        Assert.Equal("10–15.08.2021", _albums.GetAlbums().Single().Album.Subtitle);

        _albums.Delete(album.Id);

        Assert.Empty(_albums.GetAlbums());
        using var c = _db.Context.OpenConnection();
        Assert.Equal(3, c.ExecuteScalar<long>("SELECT COUNT(*) FROM Photos"));
        Assert.Equal(0, c.ExecuteScalar<long>("SELECT COUNT(*) FROM AlbumPhotos"));
    }

    [Fact]
    public void Album_DuplicateName_IsRejectedCaseInsensitive()
    {
        var mare = _albums.Create("Vacanță la mare", "10–15.08.2021");
        var munte = _albums.Create("Munte");

        Assert.True(_albums.IsNameTaken("VACANȚĂ LA MARE"));
        Assert.False(_albums.IsNameTaken("Vacanță la mare", mare.Id));   // propriul nume nu contează la editare
        Assert.Throws<InvalidOperationException>(() => _albums.Create(" vacanță la mare "));
        Assert.Throws<InvalidOperationException>(() => _albums.Update(munte.Id, "Vacanță la Mare", null));

        _albums.Update(mare.Id, "Vacanță la Mare", "");   // doar majusculele propriului nume → permis
        var updated = _albums.GetAlbums().Single(a => a.Album.Id == mare.Id).Album;
        Assert.Equal("Vacanță la Mare", updated.Name);
        Assert.Null(updated.Subtitle);
    }

    [Fact]
    public void Tag_GetTaggedPhotoIds_ReturnsPhotosWithAnyTag()
    {
        var mare = _tags.GetOrCreate("mare");
        var apus = _tags.GetOrCreate("apus");
        _tags.Assign(mare.Id, [_photoIds[0]]);
        _tags.Assign(apus.Id, [_photoIds[0], _photoIds[2]]);

        Assert.Equal(new HashSet<long> { _photoIds[0], _photoIds[2] }, _tags.GetTaggedPhotoIds());
    }

    [Fact]
    public void Album_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => _albums.Create("   "));
    }

    [Fact]
    public void Tag_GetOrCreate_IsCaseInsensitiveIncludingDiacritics()
    {
        var first = _tags.GetOrCreate("Vacanță  la   mare");
        var again = _tags.GetOrCreate("VACANȚĂ la mare");

        Assert.Equal("Vacanță la mare", first.Name);
        Assert.Equal(first.Id, again.Id);
        Assert.Single(_tags.GetTags());
    }

    [Fact]
    public void Tag_AssignUnassignDelete()
    {
        var mare = _tags.GetOrCreate("mare");
        var apus = _tags.GetOrCreate("apus");
        Assert.Equal(2, _tags.Assign(mare.Id, [_photoIds[0], _photoIds[1]]));
        Assert.Equal(0, _tags.Assign(mare.Id, [_photoIds[0]]));
        _tags.Assign(apus.Id, [_photoIds[0]]);

        Assert.Equal(["apus", "mare"], _tags.GetTagsForPhoto(_photoIds[0]).Select(t => t.Name));
        Assert.Equal(2, _tags.GetTags().Single(t => t.Tag.Name == "mare").PhotoCount);

        _tags.Unassign(mare.Id, _photoIds[0]);
        Assert.Equal(new HashSet<long> { _photoIds[1] }, _tags.GetPhotoIds(mare.Id));

        _tags.Delete(mare.Id);
        Assert.Equal(["apus"], _tags.GetTags().Select(t => t.Tag.Name));
        Assert.Empty(_tags.GetPhotoIds(mare.Id));
    }

    [Fact]
    public void Tag_RenameToExistingName_IsRejected()
    {
        var mare = _tags.GetOrCreate("mare");
        var munte = _tags.GetOrCreate("munte");

        Assert.False(_tags.TryRename(munte.Id, "Mare"));
        Assert.True(_tags.TryRename(munte.Id, "Munte"));   // doar schimbarea majusculelor pe propriul tag e permisă
        Assert.True(_tags.TryRename(mare.Id, "marea"));
        Assert.Equal(["marea", "Munte"], _tags.GetTags().Select(t => t.Tag.Name));
    }

    public void Dispose() => _db.Dispose();
}
