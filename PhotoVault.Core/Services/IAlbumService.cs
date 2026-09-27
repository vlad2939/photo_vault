using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

/// <summary>Albume manuale (§6.5): structură pur logică, fișierele de pe disc nu sunt atinse.</summary>
public interface IAlbumService
{
    IReadOnlyList<AlbumSummary> GetAlbums();
    Album Create(string name);
    void Rename(long albumId, string name);
    void Delete(long albumId);
    int AddPhotos(long albumId, IReadOnlyCollection<long> photoIds);
    int RemovePhotos(long albumId, IReadOnlyCollection<long> photoIds);
    IReadOnlySet<long> GetPhotoIds(long albumId);
    void SetCover(long albumId, long photoId);
}
