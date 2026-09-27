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

    IReadOnlyDictionary<long, int> GetRotations(IReadOnlyCollection<long> photoIds);
    void SetRotations(IReadOnlyDictionary<long, int> rotations);

    /// <summary>Nume tag-uri + nume albume, per poză (doar pozele care au cel puțin unul).</summary>
    IReadOnlyDictionary<long, string> GetSearchKeywords();

    /// <summary>Marchează pentru reîncercare miniaturile eșuate (ThumbnailPath = '') dintr-un folder sursă.</summary>
    void ResetFailedThumbnails(long sourceFolderId);

    /// <summary>Marchează / demarchează pozele ca favorite (§12.3), într-o tranzacție.</summary>
    void SetFavorite(IReadOnlyCollection<long> photoIds, bool isFavorite);

    /// <summary>Numărul de poze indexate aflate direct în folderul dat (fără subfoldere).</summary>
    int CountInFolder(string folderPath);

    /// <summary>
    /// Actualizează calea + numele pozelor redenumite pe disc (o tranzacție), păstrând Id-ul — deci albumele,
    /// tag-urile și rotirea. Întoarce câte poze indexate au fost actualizate.
    /// </summary>
    int UpdatePaths(IReadOnlyList<(string OldPath, string NewPath)> renames);
}
