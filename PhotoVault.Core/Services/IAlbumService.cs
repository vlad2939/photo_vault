using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

/// <summary>Albume manuale (§6.5): structură pur logică, fișierele de pe disc nu sunt atinse.</summary>
public interface IAlbumService
{
    IReadOnlyList<AlbumSummary> GetAlbums();
    /// <summary>true dacă există deja un album cu acest nume (fără diferență de majuscule), altul decât <paramref name="exceptAlbumId"/>.</summary>
    bool IsNameTaken(string name, long? exceptAlbumId = null);

    /// <exception cref="InvalidOperationException">Numele e deja folosit de alt album.</exception>
    Album Create(string name, string? subtitle = null);

    /// <summary>Schimbă numele și subtitlul.</summary>
    /// <exception cref="InvalidOperationException">Numele e deja folosit de alt album.</exception>
    void Update(long albumId, string name, string? subtitle);
    void Delete(long albumId);
    int AddPhotos(long albumId, IReadOnlyCollection<long> photoIds);
    int RemovePhotos(long albumId, IReadOnlyCollection<long> photoIds);
    IReadOnlySet<long> GetPhotoIds(long albumId);
    void SetCover(long albumId, long photoId);
}
