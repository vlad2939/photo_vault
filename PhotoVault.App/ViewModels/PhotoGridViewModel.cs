using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Grid-ul central de miniaturi (§6.1): toate pozele indexate sau doar cele din folderul
/// selectat în arbore (inclusiv subfolderele lui), sortate după nume.
/// </summary>
public partial class PhotoGridViewModel(IThumbnailService thumbnails, IWindowService windows) : ObservableObject
{
    private List<PhotoItemViewModel> _all = [];
    private Dictionary<long, PhotoItemViewModel> _byId = [];
    private string? _folderFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(IsLibraryEmpty), nameof(IsFolderEmpty))]
    public partial ObservableCollection<PhotoItemViewModel> Photos { get; set; } = [];

    /// <summary>Poza curentă (ultima selectată) — sursa panoului de detalii.</summary>
    [ObservableProperty]
    public partial PhotoItemViewModel? SelectedPhoto { get; set; }

    public bool IsEmpty => Photos.Count == 0;

    /// <summary>Nicio poză indexată deloc → mesajul de bun venit.</summary>
    public bool IsLibraryEmpty => _all.Count == 0;

    /// <summary>Folderul selectat nu conține poze.</summary>
    public bool IsFolderEmpty => Photos.Count == 0 && _all.Count > 0;

    /// <summary>Înlocuiește conținutul grid-ului (o singură notificare, nu zeci de mii).</summary>
    public void Load(IReadOnlyList<PhotoItem> photos)
    {
        _all = photos.Select(p => new PhotoItemViewModel(p, ToAbsolute(p.ThumbnailPath))).ToList();
        _byId = _all.ToDictionary(i => i.Id);
        ApplyFilter();
    }

    /// <summary>Filtrează după folder (inclusiv subfolderele); null = toate pozele.</summary>
    public void SetFolderFilter(string? folderPath)
    {
        _folderFilter = folderPath is null ? null : folderPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        ApplyFilter();
    }

    /// <summary>O miniatură tocmai a fost generată în fundal → cardul ei se actualizează.</summary>
    public void ApplyThumbnail(ThumbnailResult result)
    {
        if (_byId.TryGetValue(result.PhotoId, out var item))
            item.SetThumbnail(result.ThumbnailPath is null ? null : thumbnails.GetAbsolutePath(result.ThumbnailPath));
    }

    /// <summary>Dublu-click / Enter pe o poză → lightbox cu navigare prin pozele afișate în grid.</summary>
    [RelayCommand]
    private void Open(PhotoItemViewModel? photo)
    {
        photo ??= SelectedPhoto;
        if (photo is null) return;
        var index = Photos.IndexOf(photo);
        if (index < 0) return;

        var selected = windows.ShowLightbox(Photos, index);
        // După închidere, poza la care s-a ajuns rămâne selectată în grid
        if (selected is not null) SelectedPhoto = selected;
    }

    private void ApplyFilter()
    {
        var filtered = _folderFilter is null
            ? _all
            : _all.Where(p => p.FullPath.StartsWith(_folderFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        Photos = new ObservableCollection<PhotoItemViewModel>(filtered);
        if (SelectedPhoto is not null && !Photos.Contains(SelectedPhoto)) SelectedPhoto = null;
    }

    private string? ToAbsolute(string? relative) => relative switch
    {
        null => null,
        "" => string.Empty,
        _ => thumbnails.GetAbsolutePath(relative),
    };
}
