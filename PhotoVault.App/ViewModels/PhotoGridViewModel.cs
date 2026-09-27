using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;
using PhotoVault.Core.Utils;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Grid-ul central de miniaturi (§6.1): toate pozele indexate sau doar cele din folderul
/// selectat în arbore (inclusiv subfolderele lui), sortate după nume.
/// </summary>
public partial class PhotoGridViewModel(IThumbnailService thumbnails, IWindowService windows) : ObservableObject
{
    private List<PhotoItemViewModel> _all = [];
    private Dictionary<long, PhotoItemViewModel> _byId = [];
    private Func<PhotoItemViewModel, bool>? _filter;
    private string[] _searchTerms = [];
    private IReadOnlyDictionary<long, string> _keywords = new Dictionary<long, string>();
    private IReadOnlyList<PhotoItemViewModel> _selection = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(IsLibraryEmpty), nameof(IsFolderEmpty), nameof(IsSearchEmpty), nameof(CountText))]
    public partial ObservableCollection<PhotoItemViewModel> Photos { get; set; } = [];

    /// <summary>Poza curentă (ultima selectată) — sursa panoului de detalii.</summary>
    [ObservableProperty]
    public partial PhotoItemViewModel? SelectedPhoto { get; set; }

    public bool IsEmpty => Photos.Count == 0;

    public string CountText => Loc.PhotoCount(Photos.Count);

    /// <summary>Toate pozele selectate în grid (selecție multiplă: Ctrl / Shift / Ctrl+A).</summary>
    public IReadOnlyList<PhotoItemViewModel> SelectedPhotos => _selection;

    public void UpdateSelection(IEnumerable<PhotoItemViewModel> selected) => _selection = selected.ToList();

    public PhotoItemViewModel? Find(long id) => _byId.GetValueOrDefault(id);

    /// <summary>Nicio poză indexată deloc → mesajul de bun venit.</summary>
    public bool IsLibraryEmpty => _all.Count == 0;

    /// <summary>Contextul curent (folder / album / tag) nu conține poze.</summary>
    public bool IsFolderEmpty => Photos.Count == 0 && _all.Count > 0 && !IsSearching;

    /// <summary>Căutarea nu a găsit nimic în contextul curent.</summary>
    public bool IsSearchEmpty => Photos.Count == 0 && _all.Count > 0 && IsSearching;

    public bool IsSearching => _searchTerms.Length > 0;

    /// <summary>Sortare după nume fișier descendentă (Z → A); implicit ascendentă (§6.7).</summary>
    public bool SortDescending
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            ApplyFilter();
        }
    }

    /// <summary>Căutare în nume fișier + tag-uri + albume, peste contextul curent (§6.7).</summary>
    public void SetSearch(string? query)
    {
        var terms = SearchText.Terms(query);
        if (terms.SequenceEqual(_searchTerms)) return;
        _searchTerms = terms;
        ApplyFilter();
    }

    /// <summary>Textele de tag-uri / albume ale fiecărei poze (reîmprospătate după orice modificare).</summary>
    public void SetSearchKeywords(IReadOnlyDictionary<long, string> keywords)
    {
        _keywords = keywords;
        if (IsSearching) ApplyFilter();
    }

    /// <summary>Înlocuiește conținutul grid-ului (o singură notificare, nu zeci de mii).</summary>
    public void Load(IReadOnlyList<PhotoItem> photos)
    {
        _all = photos.Select(p => new PhotoItemViewModel(p, ToAbsolute(p.ThumbnailPath))).ToList();
        _byId = _all.ToDictionary(i => i.Id);
        ApplyFilter();
    }

    /// <summary>Filtrul contextului curent (folder / album / tag); null = toate pozele.</summary>
    public void SetFilter(Func<PhotoItemViewModel, bool>? filter)
    {
        _filter = filter;
        ApplyFilter();
    }

    /// <summary>Filtru pentru un folder, inclusiv subfolderele lui.</summary>
    public static Func<PhotoItemViewModel, bool> FolderFilter(string folderPath)
    {
        var prefix = folderPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return p => p.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Filtru după o mulțime de Id-uri (album, tag).</summary>
    public static Func<PhotoItemViewModel, bool> IdFilter(IReadOnlySet<long> ids) => p => ids.Contains(p.Id);

    /// <summary>Actualizează badge-ul de tag pe toate cardurile.</summary>
    public void SetTaggedPhotos(IReadOnlySet<long> taggedIds)
    {
        foreach (var photo in _all) photo.HasTags = taggedIds.Contains(photo.Id);
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
        IEnumerable<PhotoItemViewModel> filtered = _filter is null ? _all : _all.Where(_filter);
        if (IsSearching)
            filtered = filtered.Where(p => SearchText.Matches(_searchTerms, p.FileName, _keywords.GetValueOrDefault(p.Id)));
        // _all vine din DB deja sortat ascendent după nume → descendent = ordinea inversă
        if (SortDescending) filtered = filtered.Reverse();
        Photos = new ObservableCollection<PhotoItemViewModel>(filtered);
        OnPropertyChanged(nameof(IsSearching));
        if (SelectedPhoto is not null && !Photos.Contains(SelectedPhoto)) SelectedPhoto = null;
    }

    private string? ToAbsolute(string? relative) => relative switch
    {
        null => null,
        "" => string.Empty,
        _ => thumbnails.GetAbsolutePath(relative),
    };
}
