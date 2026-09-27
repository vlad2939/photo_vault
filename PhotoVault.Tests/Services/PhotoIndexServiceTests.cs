using PhotoVault.Core.Models;
using PhotoVault.Core.Services;
using PhotoVault.Data.Repositories;
using SixLabors.ImageSharp;

namespace PhotoVault.Tests.Services;

public sealed class PhotoIndexServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "PhotoVaultTests", Guid.NewGuid().ToString("N"));
    private readonly string _photos;
    private readonly string _thumbs;
    private readonly PhotoIndexService _service;
    private readonly PhotoRepository _photoRepository;

    public PhotoIndexServiceTests()
    {
        _photos = Path.Combine(_root, "Poze");
        _thumbs = Path.Combine(_root, "thumbnails");
        _photoRepository = new PhotoRepository(_db.Context);
        _service = new PhotoIndexService(
            new SourceFolderRepository(_db.Context),
            _photoRepository,
            new ThumbnailService(_thumbs, new MetadataService()));

        TestImages.WriteJpeg(Path.Combine(_photos, "b.jpg"));
        TestImages.WritePng(Path.Combine(_photos, "A.png"));
        TestImages.WriteJpeg(Path.Combine(_photos, "2024", "Iulie", "c.JPEG"), 1200, 800);
        TestImages.WriteSyntheticRaw(Path.Combine(_photos, "2024", "d.cr2"), TestImages.JpegBytes(600, 400), TestImages.JpegBytes(160, 120));
        File.WriteAllText(Path.Combine(_photos, "notite.txt"), "nu e poză");
        File.WriteAllText(Path.Combine(_photos, "stricat.jpg"), "nu e un JPEG valid");
    }

    [Fact]
    public async Task AddSourceFolder_IndexesSupportedFilesRecursively()
    {
        var reports = new List<IndexProgress>();
        var (folder, result) = await _service.AddSourceFolderAsync(_photos, new SyncProgress<IndexProgress>(reports.Add), CancellationToken.None);

        Assert.Equal(5, result.Added);   // b, A, c, d + stricat.jpg (extensie suportată)
        Assert.Equal(0, result.Removed);
        Assert.Equal(["A.png", "b.jpg", "c.JPEG", "d.cr2", "stricat.jpg"], _service.GetAllPhotos().Select(p => p.FileName));
        Assert.Equal("jpeg", _service.GetAllPhotos().Single(p => p.FileName == "c.JPEG").Extension);
        Assert.Equal(5, _service.GetPhotoCounts()[folder.Id]);
        Assert.Contains(reports, r => r.Phase == IndexPhase.Indexing && r.Current == 5 && r.Total == 5);
        Assert.NotNull(_service.GetSourceFolders().Single().LastScanned);
    }

    [Fact]
    public async Task EnsureThumbnails_GeneratesSmallJpegsAndMarksUnreadableFiles()
    {
        await _service.AddSourceFolderAsync(_photos, null, CancellationToken.None);
        var results = new List<ThumbnailResult>();

        await _service.EnsureThumbnailsAsync(null, new SyncProgress<ThumbnailResult>(results.Add), CancellationToken.None);

        Assert.Equal(5, results.Count);
        var photos = _service.GetAllPhotos();
        Assert.Equal(string.Empty, photos.Single(p => p.FileName == "stricat.jpg").ThumbnailPath);

        foreach (var photo in photos.Where(p => p.FileName != "stricat.jpg"))
        {
            Assert.False(string.IsNullOrEmpty(photo.ThumbnailPath));
            var file = Path.Combine(_thumbs, photo.ThumbnailPath!);
            using var thumb = Image.Load(file);
            Assert.True(Math.Max(thumb.Width, thumb.Height) <= 300);
        }

        // Aspect ratio păstrat: 1200×800 → 300×200; RAW 600×400 → 300×200
        using var c = Image.Load(Path.Combine(_thumbs, photos.Single(p => p.FileName == "c.JPEG").ThumbnailPath!));
        Assert.Equal((300, 200), (c.Width, c.Height));
        using var d = Image.Load(Path.Combine(_thumbs, photos.Single(p => p.FileName == "d.cr2").ThumbnailPath!));
        Assert.Equal((300, 200), (d.Width, d.Height));

        // A doua rulare nu mai are nimic de făcut
        results.Clear();
        await _service.EnsureThumbnailsAsync(null, new SyncProgress<ThumbnailResult>(results.Add), CancellationToken.None);
        Assert.Empty(results);
    }

    [Fact]
    public async Task RawThumbnail_AppliesOrientationFromRawFile()
    {
        var rotated = Path.Combine(_root, "Rotite");
        TestImages.WriteSyntheticRaw(Path.Combine(rotated, "portret.nef"), TestImages.JpegBytes(600, 400), TestImages.JpegBytes(160, 120), orientation: 6);
        await _service.AddSourceFolderAsync(rotated, null, CancellationToken.None);
        await _service.EnsureThumbnailsAsync(null, null, CancellationToken.None);

        var photo = _service.GetAllPhotos().Single();
        using var thumb = Image.Load(Path.Combine(_thumbs, photo.ThumbnailPath!));
        Assert.Equal((200, 300), (thumb.Width, thumb.Height));   // rotit 90° → portret
    }

    [Fact]
    public async Task Rescan_AddsNewAndRemovesMissingPhotos()
    {
        var (folder, _) = await _service.AddSourceFolderAsync(_photos, null, CancellationToken.None);
        await _service.EnsureThumbnailsAsync(null, null, CancellationToken.None);
        var removedThumb = Path.Combine(_thumbs, _service.GetAllPhotos().Single(p => p.FileName == "b.jpg").ThumbnailPath!);
        Assert.True(File.Exists(removedThumb));

        File.Delete(Path.Combine(_photos, "b.jpg"));
        TestImages.WriteJpeg(Path.Combine(_photos, "nou", "e.jpg"));

        var result = await _service.RescanAsync(folder.Id, null, CancellationToken.None);

        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Removed);
        Assert.DoesNotContain(_service.GetAllPhotos(), p => p.FileName == "b.jpg");
        Assert.Contains(_service.GetAllPhotos(), p => p.FileName == "e.jpg");
        Assert.False(File.Exists(removedThumb));
    }

    [Fact]
    public async Task Rescan_RetriesFailedThumbnails()
    {
        var (folder, _) = await _service.AddSourceFolderAsync(_photos, null, CancellationToken.None);
        await _service.EnsureThumbnailsAsync(null, null, CancellationToken.None);
        Assert.Equal(string.Empty, _service.GetAllPhotos().Single(p => p.FileName == "stricat.jpg").ThumbnailPath);

        // Fișierul devine lizibil (ex. copiere terminată) → re-scanarea îl reîncearcă
        TestImages.WriteJpeg(Path.Combine(_photos, "stricat.jpg"));
        await _service.RescanAsync(folder.Id, null, CancellationToken.None);
        await _service.EnsureThumbnailsAsync(null, null, CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(_service.GetAllPhotos().Single(p => p.FileName == "stricat.jpg").ThumbnailPath));
    }

    [Fact]
    public async Task Rescan_MissingFolder_ThrowsAndKeepsIndex()
    {
        var (folder, _) = await _service.AddSourceFolderAsync(_photos, null, CancellationToken.None);
        Directory.Move(_photos, _photos + "_deconectat");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => _service.RescanAsync(folder.Id, null, CancellationToken.None));
        Assert.Equal(5, _service.GetAllPhotos().Count);
    }

    [Fact]
    public async Task RemoveSourceFolder_DeletesPhotosAndThumbnailsButNotOriginals()
    {
        var (folder, _) = await _service.AddSourceFolderAsync(_photos, null, CancellationToken.None);
        await _service.EnsureThumbnailsAsync(null, null, CancellationToken.None);

        await _service.RemoveSourceFolderAsync(folder.Id);

        Assert.Empty(_service.GetAllPhotos());
        Assert.Empty(_service.GetSourceFolders());
        Assert.Empty(Directory.EnumerateFiles(_thumbs, "*.jpg", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(_photos, "A.png")));
    }

    [Fact]
    public async Task ValidateNewFolder_DetectsDuplicatesAndNesting()
    {
        await _service.AddSourceFolderAsync(_photos, null, CancellationToken.None);

        Assert.Equal(FolderValidation.AlreadyAdded, _service.ValidateNewFolder(_photos).Result);
        Assert.Equal(FolderValidation.InsideExisting, _service.ValidateNewFolder(Path.Combine(_photos, "2024")).Result);
        Assert.Equal(FolderValidation.ContainsExisting, _service.ValidateNewFolder(_root).Result);
        Assert.Equal(FolderValidation.NotFound, _service.ValidateNewFolder(Path.Combine(_root, "nu-exista")).Result);

        var other = Path.Combine(Path.GetTempPath(), "PhotoVaultTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(other);
        try
        {
            Assert.Equal(FolderValidation.Ok, _service.ValidateNewFolder(other).Result);
        }
        finally
        {
            Directory.Delete(other);
        }
    }

    public void Dispose()
    {
        _db.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    /// <summary>IProgress sincron (Progress&lt;T&gt; raportează asincron pe thread pool).</summary>
    private sealed class SyncProgress<T>(Action<T> handler) : IProgress<T>
    {
        private readonly Lock _gate = new();
        public void Report(T value) { lock (_gate) handler(value); }
    }
}
