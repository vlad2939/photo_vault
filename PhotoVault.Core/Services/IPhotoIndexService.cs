using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

/// <summary>Rezultatul validării unui folder înainte de adăugare.</summary>
public enum FolderValidation
{
    Ok,
    NotFound,
    AlreadyAdded,
    /// <summary>Folderul e deja inclus (e subfolder al unui folder sursă existent).</summary>
    InsideExisting,
    /// <summary>Folderul conține un folder sursă existent.</summary>
    ContainsExisting
}

/// <summary>Import și indexare (§6.2): foldere sursă, scanare recursivă, miniaturi.</summary>
public interface IPhotoIndexService
{
    IReadOnlyList<SourceFolder> GetSourceFolders();

    /// <summary>Numărul de poze indexate per folder sursă.</summary>
    IReadOnlyDictionary<long, int> GetPhotoCounts();

    IReadOnlyList<PhotoItem> GetAllPhotos();

    (FolderValidation Result, string? ConflictingFolder) ValidateNewFolder(string folderPath);

    /// <summary>Validarea noii locații a unui folder sursă existent (el însuși e ignorat la verificarea suprapunerilor).</summary>
    (FolderValidation Result, string? ConflictingFolder) ValidateRelocation(long sourceFolderId, string newFolderPath);

    /// <summary>Câte dintre pozele folderului există la noua locație (aceeași cale relativă) — previzualizare, fără modificări.</summary>
    Task<RelocationPreview> PreviewRelocationAsync(long sourceFolderId, string newFolderPath);

    /// <summary>
    /// Schimbă locația folderului sursă (ex. altă literă de disc, alt calculator), păstrând albumele, tag-urile și rotirile.
    /// Pozele care nu se regăsesc la noua locație rămân în index până la următoarea re-scanare.
    /// </summary>
    Task RelocateSourceFolderAsync(long sourceFolderId, string newFolderPath);

    /// <summary>Adaugă folderul și îl scanează recursiv (fără miniaturi — vezi <see cref="EnsureThumbnailsAsync"/>).</summary>
    Task<(SourceFolder Folder, ScanResult Result)> AddSourceFolderAsync(string folderPath,
        IProgress<IndexProgress>? progress, CancellationToken cancellationToken);

    /// <summary>Re-scanare manuală: indexează pozele noi și elimină din index pe cele care nu mai există pe disc.</summary>
    Task<ScanResult> RescanAsync(long sourceFolderId, IProgress<IndexProgress>? progress, CancellationToken cancellationToken);

    /// <summary>Elimină folderul și toate pozele lui din index (fișierele de pe disc rămân neatinse).</summary>
    Task RemoveSourceFolderAsync(long sourceFolderId);

    /// <summary>
    /// Generează în fundal miniaturile lipsă. Dacă generarea rulează deja, cererea
    /// e reținută și pozele noi sunt preluate în aceeași sesiune de lucru.
    /// </summary>
    Task EnsureThumbnailsAsync(IProgress<IndexProgress>? progress, IProgress<ThumbnailResult>? onThumbnail,
        CancellationToken cancellationToken);
}
