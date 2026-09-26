using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>Grid-ul central de miniaturi (§6.1). Faza 1: toate pozele indexate, sortate după nume.</summary>
public partial class PhotoGridViewModel(IThumbnailService thumbnails) : ObservableObject
{
    private Dictionary<long, PhotoItemViewModel> _byId = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<PhotoItemViewModel> Photos { get; set; } = [];

    public bool IsEmpty => Photos.Count == 0;

    /// <summary>Înlocuiește conținutul grid-ului (o singură notificare, nu zeci de mii).</summary>
    public void Load(IReadOnlyList<PhotoItem> photos)
    {
        var items = photos.Select(p => new PhotoItemViewModel(p, ToAbsolute(p.ThumbnailPath))).ToList();
        _byId = items.ToDictionary(i => i.Id);
        Photos = new ObservableCollection<PhotoItemViewModel>(items);
    }

    /// <summary>O miniatură tocmai a fost generată în fundal → cardul ei se actualizează.</summary>
    public void ApplyThumbnail(ThumbnailResult result)
    {
        if (_byId.TryGetValue(result.PhotoId, out var item))
            item.SetThumbnail(result.ThumbnailPath is null ? null : thumbnails.GetAbsolutePath(result.ThumbnailPath));
    }

    private string? ToAbsolute(string? relative) => relative switch
    {
        null => null,
        "" => string.Empty,
        _ => thumbnails.GetAbsolutePath(relative),
    };
}
