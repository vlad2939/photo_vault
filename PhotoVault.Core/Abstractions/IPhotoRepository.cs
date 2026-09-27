using PhotoVault.Core.Models;

namespace PhotoVault.Core.Abstractions;

public interface IPhotoRepository
{
    /// <summary>Toate pozele indexate, ordonate după nume fișier.</summary>
    IReadOnlyList<PhotoItem> GetAll();

    int Count();

    /// <summary>Numărul de poze per folder sursă.</summary>
    IReadOnlyDictionary<long, int> CountBySourceFolder();

    /// <summary>Id + cale completă + miniatură pentru pozele unui folder sursă (folosit la re-scanare).</summary>
    IReadOnlyList<PhotoItem> GetBySourceFolder(long sourceFolderId);

    /// <summary>Pozele fără miniatură generată (ThumbnailPath NULL).</summary>
    IReadOnlyList<PhotoItem> GetWithoutThumbnail();

    /// <summary>Inserează pozele într-o singură tranzacție; setează Id-urile generate.</summary>
    void InsertMany(IReadOnlyList<PhotoItem> photos);

    void DeleteMany(IReadOnlyCollection<long> ids);

    /// <summary>Salvează rezultatele generării de miniaturi (o tranzacție pentru tot lotul).</summary>
    void UpdateThumbnails(IReadOnlyCollection<ThumbnailResult> results);

    /// <summary>Marchează pentru reîncercare miniaturile eșuate (ThumbnailPath = '') dintr-un folder sursă.</summary>
    void ResetFailedThumbnails(long sourceFolderId);
}
