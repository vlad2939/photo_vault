using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

/// <summary>
/// Orchestrarea importului: scanare recursivă a folderelor sursă, sincronizarea
/// tabelului Photos cu discul și generarea miniaturilor pe un pool de thread-uri.
/// Fișierele originale sunt doar citite — niciodată mutate sau modificate.
/// </summary>
public sealed class PhotoIndexService(
    ISourceFolderRepository folders,
    IPhotoRepository photos,
    IThumbnailService thumbnails) : IPhotoIndexService
{
    private const int InsertBatchSize = 500;
    private const int ProgressStep = 20;
    private const int ThumbnailFlushSize = 64;

    private static readonly EnumerationOptions ScanOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        ReturnSpecialDirectories = false,
    };

    // Scanările (adăugare / re-scanare / eliminare) rulează pe rând.
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    private readonly Lock _thumbnailGate = new();
    private Task? _thumbnailTask;
    private bool _thumbnailRerunRequested;

    public IReadOnlyList<SourceFolder> GetSourceFolders() => folders.GetAll();

    public IReadOnlyDictionary<long, int> GetPhotoCounts() => photos.CountBySourceFolder();

    public IReadOnlyList<PhotoItem> GetAllPhotos() => photos.GetAll();

    public (FolderValidation Result, string? ConflictingFolder) ValidateNewFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return (FolderValidation.NotFound, null);

        var candidate = WithTrailingSeparator(Path.GetFullPath(folderPath));
        foreach (var existing in folders.GetAll())
        {
            var current = WithTrailingSeparator(Path.GetFullPath(existing.FolderPath));
            if (string.Equals(candidate, current, StringComparison.OrdinalIgnoreCase))
                return (FolderValidation.AlreadyAdded, existing.FolderPath);
            if (candidate.StartsWith(current, StringComparison.OrdinalIgnoreCase))
                return (FolderValidation.InsideExisting, existing.FolderPath);
            if (current.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                return (FolderValidation.ContainsExisting, existing.FolderPath);
        }
        return (FolderValidation.Ok, null);
    }

    public async Task<(SourceFolder Folder, ScanResult Result)> AddSourceFolderAsync(string folderPath,
        IProgress<IndexProgress>? progress, CancellationToken cancellationToken)
    {
        await _scanLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                var folder = folders.Add(Path.GetFullPath(folderPath));
                var result = Synchronize(folder, progress, cancellationToken);
                return (folder, result);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _scanLock.Release();
        }
    }

    public async Task<ScanResult> RescanAsync(long sourceFolderId, IProgress<IndexProgress>? progress,
        CancellationToken cancellationToken)
    {
        await _scanLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var folder = folders.GetById(sourceFolderId)
                         ?? throw new InvalidOperationException($"Folderul sursă {sourceFolderId} nu există.");

            // Un folder inaccesibil (disc extern deconectat) NU înseamnă poze șterse:
            // nu golim indexul, apelantul informează utilizatorul.
            if (!Directory.Exists(folder.FolderPath))
                throw new DirectoryNotFoundException(folder.FolderPath);

            return await Task.Run(() => Synchronize(folder, progress, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _scanLock.Release();
        }
    }

    public async Task RemoveSourceFolderAsync(long sourceFolderId)
    {
        await _scanLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(() =>
            {
                var thumbnailPaths = photos.GetBySourceFolder(sourceFolderId).Select(p => p.ThumbnailPath).ToList();
                folders.Delete(sourceFolderId);   // ON DELETE CASCADE → Photos, AlbumPhotos, PhotoTags
                foreach (var path in thumbnailPaths) thumbnails.Delete(path);
            }).ConfigureAwait(false);
        }
        finally
        {
            _scanLock.Release();
        }
    }

    public Task EnsureThumbnailsAsync(IProgress<IndexProgress>? progress, IProgress<ThumbnailResult>? onThumbnail,
        CancellationToken cancellationToken)
    {
        lock (_thumbnailGate)
        {
            if (_thumbnailTask is not null)
            {
                _thumbnailRerunRequested = true;
                return _thumbnailTask;
            }
            _thumbnailTask = Task.Run(() => RunThumbnailLoopAsync(progress, onThumbnail, cancellationToken), cancellationToken);
            return _thumbnailTask;
        }
    }

    private async Task RunThumbnailLoopAsync(IProgress<IndexProgress>? progress, IProgress<ThumbnailResult>? onThumbnail,
        CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var pending = photos.GetWithoutThumbnail();
                if (pending.Count > 0)
                    await GenerateThumbnailsAsync(pending, progress, onThumbnail, cancellationToken).ConfigureAwait(false);

                lock (_thumbnailGate)
                {
                    if (!_thumbnailRerunRequested)
                    {
                        _thumbnailTask = null;
                        return;
                    }
                    _thumbnailRerunRequested = false;
                }
            }
        }
        catch
        {
            lock (_thumbnailGate) _thumbnailTask = null;
            throw;
        }
    }

    private async Task GenerateThumbnailsAsync(IReadOnlyList<PhotoItem> pending, IProgress<IndexProgress>? progress,
        IProgress<ThumbnailResult>? onThumbnail, CancellationToken cancellationToken)
    {
        var total = pending.Count;
        var done = 0;
        var buffer = new List<ThumbnailResult>(ThumbnailFlushSize);
        var bufferLock = new Lock();
        var writeLock = new Lock();   // un singur scriitor SQLite la un moment dat
        progress?.Report(new IndexProgress(IndexPhase.Thumbnails, 0, total));

        // Rezultatele sunt anunțate UI-ului abia după ce au fost salvate în DB: o reîncărcare
        // a grid-ului din DB nu poate „pierde" astfel o miniatură deja generată.
        void Flush(List<ThumbnailResult> batch)
        {
            lock (writeLock) photos.UpdateThumbnails(batch);
            if (onThumbnail is null) return;
            foreach (var result in batch) onThumbnail.Report(result);
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount - 1, 1, 6),
            CancellationToken = cancellationToken,
        };

        try
        {
            await Parallel.ForEachAsync(pending, options, (photo, _) =>
            {
                var result = thumbnails.Generate(photo);
                List<ThumbnailResult>? toFlush = null;
                lock (bufferLock)
                {
                    buffer.Add(result);
                    // Loturi mici la început (primele miniaturi apar repede), apoi mai mari (mai puține tranzacții)
                    if (buffer.Count >= (done < ThumbnailFlushSize ? 8 : ThumbnailFlushSize))
                    {
                        toFlush = [.. buffer];
                        buffer.Clear();
                    }
                }
                if (toFlush is not null) Flush(toFlush);

                // Progresul se raportează din 20 în 20 (+ ultimul): la zeci de mii de poze, UI-ul nu e inundat de mesaje
                var current = Interlocked.Increment(ref done);
                if (current % ProgressStep == 0 || current == total)
                    progress?.Report(new IndexProgress(IndexPhase.Thumbnails, current, total));
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
        }
        finally
        {
            // Și la anulare (închiderea aplicației) salvăm ce s-a generat deja.
            List<ThumbnailResult> rest;
            lock (bufferLock)
            {
                rest = [.. buffer];
                buffer.Clear();
            }
            if (rest.Count > 0) Flush(rest);
        }
    }

    /// <summary>Aduce indexul unui folder în acord cu discul: adaugă pozele noi, elimină pozele dispărute.</summary>
    private ScanResult Synchronize(SourceFolder folder, IProgress<IndexProgress>? progress, CancellationToken cancellationToken)
    {
        progress?.Report(new IndexProgress(IndexPhase.Scanning, 0, 0));

        var onDisk = new List<(string Path, string Extension)>();
        foreach (var file in Directory.EnumerateFiles(folder.FolderPath, "*", ScanOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var extension = SupportedFormats.NormalizeExtension(file);
            if (extension is not null) onDisk.Add((file, extension));
        }

        var indexed = photos.GetBySourceFolder(folder.Id);
        var indexedPaths = indexed.Select(p => p.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var diskPaths = onDisk.Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newFiles = onDisk.Where(f => !indexedPaths.Contains(f.Path)).ToList();
        var missing = indexed.Where(p => !diskPaths.Contains(p.FullPath)).ToList();

        var now = DateTime.Now;
        progress?.Report(new IndexProgress(IndexPhase.Indexing, 0, newFiles.Count));
        for (var start = 0; start < newFiles.Count; start += InsertBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = newFiles.Skip(start).Take(InsertBatchSize).Select(f => new PhotoItem
            {
                SourceFolderId = folder.Id,
                FullPath = f.Path,
                FileName = Path.GetFileName(f.Path),
                Extension = f.Extension,
                FileSizeBytes = TryGetSize(f.Path),
                DateAdded = now,
            }).ToList();
            photos.InsertMany(batch);
            progress?.Report(new IndexProgress(IndexPhase.Indexing, start + batch.Count, newFiles.Count));
        }

        // Poze șterse/mutate extern: eliminate direct din index, fără confirmare (§4)
        if (missing.Count > 0)
        {
            photos.DeleteMany(missing.Select(p => p.Id).ToList());
            foreach (var photo in missing) thumbnails.Delete(photo.ThumbnailPath);
        }

        // Re-scanarea reîncearcă și miniaturile eșuate (ex. fișier blocat temporar la indexarea anterioară)
        photos.ResetFailedThumbnails(folder.Id);
        folders.UpdateLastScanned(folder.Id, now);
        return new ScanResult(newFiles.Count, missing.Count, onDisk.Count);
    }

    private static long? TryGetSize(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string WithTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
