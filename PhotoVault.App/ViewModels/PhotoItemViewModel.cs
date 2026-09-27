using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// O poză din grid. Miniatura se încarcă leneș, doar când cardul devine vizibil;
/// VM-ul ține doar o referință slabă la imagine, ca memoria să rămână mică la 50.000 de poze.
/// </summary>
public partial class PhotoItemViewModel : ObservableObject
{
    private readonly PhotoItem _photo;
    private WeakReference<ImageSource>? _thumbnail;
    private bool _loading;

    public PhotoItemViewModel(PhotoItem photo, string? thumbnailAbsolutePath)
    {
        _photo = photo;
        ThumbnailAbsolutePath = thumbnailAbsolutePath;
    }

    public PhotoItem Model => _photo;
    public long Id => _photo.Id;
    public long SourceFolderId => _photo.SourceFolderId;
    public string FileName => _photo.FileName;
    public string FullPath => _photo.FullPath;
    public string Extension => _photo.Extension;
    public long? FileSizeBytes => _photo.FileSizeBytes;
    public int RotationDegrees => _photo.RotationDegrees;

    /// <summary>Eticheta de format de pe card (JPG, PNG, CR2...).</summary>
    public string FormatLabel => _photo.Extension == "jpeg" ? "JPG" : _photo.Extension.ToUpperInvariant();

    /// <summary>Calea absolută a miniaturii; "" = imagine ilizibilă; null = încă negenerată.</summary>
    public string? ThumbnailAbsolutePath
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value)) return;
            _thumbnail = null;
            OnPropertyChanged(nameof(IsThumbnailUnavailable));
            OnPropertyChanged(nameof(Thumbnail));
        }
    }

    /// <summary>Fișierul nu a putut fi citit (nici previzualizare RAW) → iconiță de rezervă.</summary>
    public bool IsThumbnailUnavailable => ThumbnailAbsolutePath == string.Empty;

    public ImageSource? Thumbnail
    {
        get
        {
            if (_thumbnail is not null && _thumbnail.TryGetTarget(out var image)) return image;
            if (!_loading && !string.IsNullOrEmpty(ThumbnailAbsolutePath)) _ = LoadThumbnailAsync(ThumbnailAbsolutePath);
            return null;
        }
    }

    public void SetThumbnail(string? absolutePath) => ThumbnailAbsolutePath = absolutePath ?? string.Empty;

    private async Task LoadThumbnailAsync(string path)
    {
        _loading = true;
        try
        {
            var image = await ThumbnailCache.GetAsync(path);
            if (image is null || path != ThumbnailAbsolutePath) return;
            _thumbnail = new WeakReference<ImageSource>(image);
            OnPropertyChanged(nameof(Thumbnail));
        }
        finally
        {
            _loading = false;
        }
    }
}
