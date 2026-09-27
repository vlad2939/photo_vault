using System.Diagnostics;
using Dapper;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;
using PhotoVault.Core.Utils;
using PhotoVault.Data.Repositories;

namespace PhotoVault.Tests.Repositories;

/// <summary>
/// Protecție la regresii de performanță pe o bibliotecă mare (§9 Faza 7: 50.000 de poze).
/// Limitele sunt generoase (de ~20× peste timpii măsurați), ca testul să nu fie instabil pe CI.
/// </summary>
public sealed class LargeLibraryTests : IDisposable
{
    private const int PhotoCount = 50_000;
    private readonly TestDatabase _db = new();
    private readonly PhotoRepository _photos;
    private readonly List<PhotoItem> _items;

    public LargeLibraryTests()
    {
        _photos = new PhotoRepository(_db.Context);
        using (var c = _db.Context.OpenConnection())
            c.Execute("INSERT INTO SourceFolders (FolderPath, DateAdded) VALUES ('C:\\Poze', '2026-01-01T00:00:00')");
        _items = Enumerable.Range(1, PhotoCount).Select(i => new PhotoItem
        {
            SourceFolderId = 1,
            FullPath = $"C:\\Poze\\{2000 + i % 30}\\IMG_{i:D6}.jpg",
            FileName = $"IMG_{i:D6}.jpg",
            Extension = "jpg",
            DateAdded = DateTime.Now,
        }).ToList();
        foreach (var chunk in _items.Chunk(500)) _photos.InsertMany(chunk);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void KeywordsSearchAndDelete_StayFast()
    {
        var tags = new TagService(new TagRepository(_db.Context));
        var albums = new AlbumService(new AlbumRepository(_db.Context));
        tags.Assign(tags.GetOrCreate("Mare").Id, _items.Take(5_000).Select(p => p.Id).ToList());
        albums.AddPhotos(albums.Create("Vacanță Grecia").Id, _items.Skip(2_000).Take(8_000).Select(p => p.Id).ToList());

        var sw = Stopwatch.StartNew();
        var keywords = _photos.GetSearchKeywords();
        var keywordsTime = sw.Elapsed;
        Assert.Equal(10_000, keywords.Count);
        Assert.Contains("Mare", keywords[_items[3_000].Id]);
        Assert.Contains("Vacanță Grecia", keywords[_items[3_000].Id]);
        Assert.True(keywordsTime < TimeSpan.FromSeconds(3), $"GetSearchKeywords: {keywordsTime.TotalMilliseconds:0} ms");

        // Căutare la o tastă: termeni + câmpuri normalizate o singură dată
        var names = _items.Select(p => SearchText.Normalize(p.FileName)).ToArray();
        var normalized = keywords.ToDictionary(kv => kv.Key, kv => SearchText.Normalize(kv.Value));
        var terms = SearchText.Terms("grecia img_0030");
        sw.Restart();
        var matches = _items.Where((p, i) => SearchText.MatchesNormalized(terms, names[i], normalized.GetValueOrDefault(p.Id))).Count();
        Assert.Equal(100, matches);   // IMG_003000 … IMG_003099, toate în album
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1), $"Căutare: {sw.Elapsed.TotalMilliseconds:0} ms");

        sw.Restart();
        _photos.DeleteMany(_items.Take(20_000).Select(p => p.Id).ToList());
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"DeleteMany: {sw.Elapsed.TotalMilliseconds:0} ms");
        Assert.Equal(PhotoCount - 20_000, _photos.Count());
    }
}
