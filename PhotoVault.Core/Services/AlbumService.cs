using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

public sealed class AlbumService(IAlbumRepository albums, IDatabaseBackup? backup = null) : IAlbumService
{
    public IReadOnlyList<AlbumSummary> GetAlbums() => albums.GetAll();

    public bool IsNameTaken(string name, long? exceptAlbumId = null)
    {
        var normalized = NormalizeName(name);
        return albums.GetAll().Any(a => a.Album.Id != exceptAlbumId &&
                                        string.Equals(a.Album.Name, normalized, StringComparison.CurrentCultureIgnoreCase));
    }

    public Album Create(string name, string? subtitle = null)
    {
        if (IsNameTaken(name)) throw new InvalidOperationException($"Există deja un album numit „{name.Trim()}”.");
        return albums.Create(NormalizeName(name), NormalizeSubtitle(subtitle));
    }

    public void Update(long albumId, string name, string? subtitle)
    {
        if (IsNameTaken(name, albumId)) throw new InvalidOperationException($"Există deja un album numit „{name.Trim()}”.");
        albums.Update(albumId, NormalizeName(name), NormalizeSubtitle(subtitle));
    }

    public void Delete(long albumId)
    {
        backup?.CreateBackup();   // §12.1: ștergerea unui album e ireversibilă
        albums.Delete(albumId);
    }

    public int AddPhotos(long albumId, IReadOnlyCollection<long> photoIds) =>
        photoIds.Count == 0 ? 0 : albums.AddPhotos(albumId, photoIds);

    public int RemovePhotos(long albumId, IReadOnlyCollection<long> photoIds) =>
        photoIds.Count == 0 ? 0 : albums.RemovePhotos(albumId, photoIds);

    public IReadOnlySet<long> GetPhotoIds(long albumId) => albums.GetPhotoIds(albumId);

    public void SetCover(long albumId, long photoId) => albums.SetCover(albumId, photoId);

    private static string? NormalizeSubtitle(string? subtitle) =>
        string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim();

    /// <summary>Numele nu pot fi goale; unicitatea e verificată de <see cref="IsNameTaken"/>.</summary>
    private static string NormalizeName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("Numele albumului nu poate fi gol.", nameof(name));
        return trimmed;
    }
}
