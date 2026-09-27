using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Lightbox (§6.4): navigare prin pozele afișate în grid. Cât timp imaginea mare se
/// decodează, se afișează miniatura (deja în cache) — trecerea e instantanee vizual.
/// Imaginea următoare e pre-încărcată în fundal.
/// </summary>
public partial class LightboxViewModel : ObservableObject
{
    private readonly IReadOnlyList<PhotoItemViewModel> _photos;
    private readonly IMetadataService _metadata;
    private readonly Dictionary<long, Task<ImageSource?>> _loads = [];
    private int _version;

    public LightboxViewModel(IReadOnlyList<PhotoItemViewModel> photos, int startIndex, IMetadataService metadata)
    {
        _photos = photos;
        _metadata = metadata;
        Index = Math.Clamp(startIndex, 0, Math.Max(0, photos.Count - 1));
        _ = ShowCurrentAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Current), nameof(CounterText))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand), nameof(PreviousCommand))]
    public partial int Index { get; set; }

    public PhotoItemViewModel? Current => _photos.Count > 0 ? _photos[Index] : null;

    public int Count => _photos.Count;

    public string CounterText => $"{Loc.Number(Index + 1)} / {Loc.Number(_photos.Count)}";

    /// <summary>Imaginea la rezoluție mare (null cât timp se încarcă).</summary>
    [ObservableProperty]
    public partial ImageSource? Image { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool LoadFailed { get; set; }

    private bool CanGoNext() => Index < _photos.Count - 1;
    private bool CanGoPrevious() => Index > 0;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next() => GoTo(Index + 1);

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void Previous() => GoTo(Index - 1);

    public void GoTo(int index)
    {
        if (index < 0 || index >= _photos.Count || index == Index) return;
        Index = index;
        _ = ShowCurrentAsync();
    }

    private async Task ShowCurrentAsync()
    {
        var current = Current;
        if (current is null) return;
        var version = ++_version;

        Image = null;
        LoadFailed = false;
        IsLoading = true;
        var image = await LoadAsync(current);
        if (version != _version) return;   // utilizatorul a navigat între timp

        Image = image;
        IsLoading = false;
        LoadFailed = image is null;

        // Pre-încărcare: următoarea poză (direcția uzuală de navigare); cache mic, doar vecinii
        if (CanGoNext()) _ = LoadAsync(_photos[Index + 1]);
        TrimCache();
    }

    private Task<ImageSource?> LoadAsync(PhotoItemViewModel photo)
    {
        if (!_loads.TryGetValue(photo.Id, out var task))
        {
            task = LoadCoreAsync(photo);
            _loads[photo.Id] = task;
        }
        return task;
    }

    private async Task<ImageSource?> LoadCoreAsync(PhotoItemViewModel photo) =>
        await FullImageLoader.LoadAsync(photo.Model, _metadata);

    private void TrimCache()
    {
        var keep = new HashSet<long>();
        for (var i = Math.Max(0, Index - 1); i <= Math.Min(_photos.Count - 1, Index + 1); i++) keep.Add(_photos[i].Id);
        foreach (var id in _loads.Keys.Where(id => !keep.Contains(id)).ToList()) _loads.Remove(id);
    }
}
