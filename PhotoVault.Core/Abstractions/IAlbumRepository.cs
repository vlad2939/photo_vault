using PhotoVault.Core.Models;

namespace PhotoVault.Core.Abstractions;

public interface IAlbumRepository
{
    IReadOnlyList<AlbumSummary> GetAll();
    Album Create(string name, string? subtitle);
    void Update(long albumId, string name, string? subtitle);

    /// <summary>Șterge doar structura logică (Albums + AlbumPhotos); pozele rămân indexate.</summary>
    void Delete(long albumId);

    /// <summary>Adaugă pozele (cele deja prezente sunt ignorate); întoarce câte au fost adăugate efectiv.</summary>
    int AddPhotos(long albumId, IReadOnlyCollection<long> photoIds);

    int RemovePhotos(long albumId, IReadOnlyCollection<long> photoIds);
    IReadOnlySet<long> GetPhotoIds(long albumId);
    void SetCover(long albumId, long? photoId);
}
