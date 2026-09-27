using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

public sealed class AlbumService(IAlbumRepository albums) : IAlbumService
{
    public IReadOnlyList<AlbumSummary> GetAlbums() => albums.GetAll();

    public Album Create(string name) => albums.Create(NormalizeName(name));

    public void Rename(long albumId, string name) => albums.Rename(albumId, NormalizeName(name));

    public void Delete(long albumId) => albums.Delete(albumId);

    public int AddPhotos(long albumId, IReadOnlyCollection<long> photoIds) =>
        photoIds.Count == 0 ? 0 : albums.AddPhotos(albumId, photoIds);

    public int RemovePhotos(long albumId, IReadOnlyCollection<long> photoIds) =>
        photoIds.Count == 0 ? 0 : albums.RemovePhotos(albumId, photoIds);

    public IReadOnlySet<long> GetPhotoIds(long albumId) => albums.GetPhotoIds(albumId);

    public void SetCover(long albumId, long photoId) => albums.SetCover(albumId, photoId);

    /// <summary>Numele albumelor pot repeta (nu sunt unice în schemă), dar nu pot fi goale.</summary>
    private static string NormalizeName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("Numele albumului nu poate fi gol.", nameof(name));
        return trimmed;
    }
}
