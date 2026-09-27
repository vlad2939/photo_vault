using Dapper;
using PhotoVault.Core.Services;
using PhotoVault.Core.Utils;
using PhotoVault.Data.Repositories;

namespace PhotoVault.Tests.Services;

public sealed class PhotoServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly PhotoService _photos;
    private readonly long[] _ids;

    public PhotoServiceTests()
    {
        _photos = new PhotoService(new PhotoRepository(_db.Context));
        using var c = _db.Context.OpenConnection();
        c.Execute("INSERT INTO SourceFolders (FolderPath, DateAdded) VALUES ('C:\\Poze', '2026-01-01T00:00:00')");
        foreach (var name in new[] { "a.jpg", "b.jpg", "c.jpg" })
            c.Execute("INSERT INTO Photos (SourceFolderId, FullPath, FileName, Extension, DateAdded) VALUES (1, @path, @name, 'jpg', '2026-01-01T00:00:00')",
                new { path = "C:\\Poze\\" + name, name });
        _ids = c.Query<long>("SELECT Id FROM Photos ORDER BY FileName").ToArray();
    }

    [Fact]
    public void RotateClockwise_CyclesThrough0_90_180_270()
    {
        Assert.Equal(90, _photos.RotateClockwise([_ids[0]])[_ids[0]]);
        _photos.RotateClockwise([_ids[0], _ids[1]]);
        _photos.RotateClockwise([_ids[0]]);
        var result = _photos.RotateClockwise([_ids[0]]);

        Assert.Equal(0, result[_ids[0]]);   // 90 → 180 → 270 → 0
        using var c = _db.Context.OpenConnection();
        Assert.Equal(90, c.ExecuteScalar<long>("SELECT RotationDegrees FROM Photos WHERE Id = @id", new { id = _ids[1] }));
        Assert.Equal(0, c.ExecuteScalar<long>("SELECT RotationDegrees FROM Photos WHERE Id = @id", new { id = _ids[2] }));
    }

    [Fact]
    public void GetSearchKeywords_CombinesTagAndAlbumNames()
    {
        var tags = new TagService(new TagRepository(_db.Context));
        var albums = new AlbumService(new AlbumRepository(_db.Context));
        var mare = tags.GetOrCreate("mare");
        tags.Assign(mare.Id, [_ids[0], _ids[1]]);
        var album = albums.Create("Vacanță Grecia");
        albums.AddPhotos(album.Id, [_ids[0]]);

        var keywords = _photos.GetSearchKeywords();

        Assert.Equal("mare Vacanță Grecia", keywords[_ids[0]]);
        Assert.Equal("mare", keywords[_ids[1]]);
        Assert.False(keywords.ContainsKey(_ids[2]));
    }

    [Theory]
    [InlineData("vacanta", true)]           // fără diacritice
    [InlineData("GRECIA", true)]            // fără majuscule
    [InlineData("dsc vacanță", true)]       // toți termenii, în câmpuri diferite
    [InlineData("dsc munte", false)]        // un termen lipsă → nu se potrivește
    [InlineData("0042", true)]
    public void SearchText_MatchesAllTermsIgnoringCaseAndDiacritics(string query, bool expected)
    {
        Assert.Equal(expected, SearchText.Matches(SearchText.Terms(query), "DSC_0042.JPG", "mare Vacanță Grecia"));
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void SetFavorite_PersistsAndIsLoadedWithPhotos()
    {
        _photos.SetFavorite([_ids[0], _ids[2]], true);
        var repository = new PhotoRepository(_db.Context);
        Assert.Equal([true, false, true], repository.GetAll().Select(p => p.IsFavorite));

        _photos.SetFavorite([_ids[0]], false);
        Assert.Equal([false, false, true], repository.GetAll().Select(p => p.IsFavorite));
    }
}
