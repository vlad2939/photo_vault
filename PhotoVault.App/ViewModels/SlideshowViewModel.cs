using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>O poză pregătită pentru afișare: imaginea + mișcarea Ken Burns aleasă pentru ea.</summary>
public sealed record SlideshowSlide(PhotoItemViewModel Photo, ImageSource Image, KenBurnsMotion Motion);

/// <summary>
/// Slideshow (§6.10): parcurge pozele afișate în grid, fiecare cu mișcare Ken Burns aleatorie și tranziție fade,
/// cu muzica din playlist în buclă. Animațiile propriu-zise sunt în <c>SlideshowWindow</c>; aici sunt ritmul,
/// navigarea și pre-încărcarea imaginilor.
/// </summary>
public partial class SlideshowViewModel : ObservableObject, IDisposable
{
    private readonly IReadOnlyList<PhotoItemViewModel> _photos;
    private readonly IMetadataService _metadata;
    private readonly ISlideshowService _slideshow;
    private readonly IMusicPlayer _music;
    private readonly ISettingsService _settingsService;
    private readonly IFilePicker _filePicker;
    private AppSettings Settings => _settingsService.Current;
    private readonly DispatcherTimer _advance = new();
    private readonly Dictionary<long, Task<ImageSource?>> _loads = [];
    private int _version;
    private bool _musicStarted;
    private int _unreadableInRow;

    public SlideshowViewModel(IReadOnlyList<PhotoItemViewModel> photos, int startIndex, string title, ISettingsService settings,
        IFilePicker filePicker,
        IMetadataService metadata, ISlideshowService slideshow, IMusicPlayer music)
    {
        _photos = photos;
        _metadata = metadata;
        _slideshow = slideshow;
        _music = music;
        _settingsService = settings;
        _filePicker = filePicker;
        _music.TrackStarted += name => NowPlaying = name;
        Title = title;
        Index = Math.Clamp(startIndex, 0, Math.Max(0, photos.Count - 1));

        // O poză stă vizibilă „durata afișare", plus tranziția fade spre următoarea
        _advance.Interval = DisplayDuration + FadeDuration;
        _advance.Tick += (_, _) => Advance();
    }

    /// <summary>Poza nouă e gata de afișat (fereastra pornește fade-ul și mișcarea Ken Burns).</summary>
    public event Action<SlideshowSlide>? SlideReady;

    /// <summary>Slideshow-ul s-a terminat sau utilizatorul a ieșit.</summary>
    public event Action? CloseRequested;

    public string Title { get; }
    public int Count => _photos.Count;
    public TimeSpan DisplayDuration => TimeSpan.FromSeconds(Settings.SlideshowDurationSec);
    public TimeSpan FadeDuration => TimeSpan.FromMilliseconds(Settings.SlideshowFadeMs);

    /// <summary>Mișcarea Ken Burns durează toată prezența pozei pe ecran: fade-in + afișare + fade-out.</summary>
    public TimeSpan MotionDuration => DisplayDuration + FadeDuration + FadeDuration;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CounterText), nameof(Current))]
    public partial int Index { get; set; }

    public PhotoItemViewModel? Current => _photos.Count > 0 ? _photos[Index] : null;

    public string CounterText => $"{Loc.Number(Index + 1)} / {Loc.Number(_photos.Count)}";

    /// <summary>Numele pozei afișate efectiv (se schimbă odată cu imaginea, nu la începutul încărcării).</summary>
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPlaying { get; set; } = true;

    /// <summary>Redare în buclă (reia de la prima poză) sau o singură dată (se încheie după ultima); salvată în setări.</summary>
    public bool IsLooping
    {
        get => Settings.SlideshowLoop;
        set
        {
            if (Settings.SlideshowLoop == value) return;
            Settings.SlideshowLoop = value;
            _settingsService.Save();
            OnPropertyChanged();
            // Poza următoare (inclusiv prima, dacă suntem la ultima) începe să se încarce din timp
            if (NextIndex is { } next) _ = LoadAsync(_photos[next]);
        }
    }

    [RelayCommand]
    private void ToggleLoop() => IsLooping = !IsLooping;

    /// <summary>Poza care urmează la redarea automată; null după ultima dacă nu e buclă.</summary>
    private int? NextIndex =>
        Index + 1 < _photos.Count ? Index + 1
        : IsLooping && _photos.Count > 1 ? 0
        : null;

    /// <summary>Pornește slideshow-ul (după afișarea ferestrei).</summary>
    public void Start()
    {
        _musicStarted = _music.Start(Settings.SlideshowPlaylistPaths, Settings.SlideshowVolume);
        _ = ShowCurrentAsync();
    }

    [RelayCommand]
    private void TogglePlay()
    {
        IsPlaying = !IsPlaying;
        if (IsPlaying)
        {
            _advance.Start();
            if (_musicStarted) _music.Resume();
        }
        else
        {
            _advance.Stop();
            if (_musicStarted) _music.Pause();
        }
    }

    [RelayCommand]
    private void Next()
    {
        if (Index < _photos.Count - 1) GoTo(Index + 1);
        else GoTo(0);   // navigare manuală: de la ultima revine la prima
    }

    [RelayCommand]
    private void Previous() => GoTo(Index > 0 ? Index - 1 : _photos.Count - 1);

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke();

    /// <summary>Piesa redată acum (afișată discret deasupra barei de control); gol fără muzică.</summary>
    [ObservableProperty]
    public partial string NowPlaying { get; set; } = string.Empty;

    /// <summary>
    /// Butonul „Muzică" din bara de control: alegerea uneia sau mai multor piese MP3. Piesele se adaugă în playlist-ul
    /// salvat (același din Opțiuni → Slideshow) și redarea pornește imediat cu prima piesă aleasă.
    /// </summary>
    [RelayCommand]
    private void AddMusic()
    {
        // Poza nu avansează cât timp dialogul e deschis
        _advance.Stop();
        var files = _filePicker.PickFiles(Loc.Get("Str.Settings.MusicPick"), Loc.Get("Str.Settings.MusicFilter") + "|*.mp3");
        if (files.Count > 0)
        {
            var playlist = Settings.SlideshowPlaylistPaths;
            foreach (var file in files)
                if (!playlist.Contains(file, StringComparer.OrdinalIgnoreCase)) playlist.Add(file);
            _settingsService.Save();

            _musicStarted = _music.Start(playlist, Settings.SlideshowVolume, files[0]);
            if (!_musicStarted) NowPlaying = Loc.Get("Str.Slideshow.MusicUnavailable");
            else if (!IsPlaying) _music.Pause();
        }
        if (IsPlaying) _advance.Start();
    }

    private void Advance()
    {
        // Redare automată: după ultima poză slideshow-ul reia de la prima (buclă) sau se încheie
        if (NextIndex is not { } next)
        {
            _advance.Stop();
            CloseRequested?.Invoke();
            return;
        }
        GoTo(next);
    }

    private void GoTo(int index)
    {
        if (_photos.Count == 0 || index < 0 || index >= _photos.Count) return;
        Index = index;
        _ = ShowCurrentAsync();
    }

    private async Task ShowCurrentAsync()
    {
        var version = ++_version;
        _advance.Stop();
        if (Current is not { } photo) return;

        var image = await LoadAsync(photo) ?? photo.Thumbnail;
        if (version != _version) return;   // utilizatorul a navigat între timp

        // Pre-încărcare: poza următoare e decodată cât timp aceasta e pe ecran
        TrimCache();
        if (NextIndex is { } next) _ = LoadAsync(_photos[next]);

        if (image is null)
        {
            // Imagine ilizibilă (detaliile sunt deja în logs/) → se trece imediat mai departe;
            // în buclă, dacă nicio poză nu se poate citi, slideshow-ul se oprește în loc să se rotească la nesfârșit
            if (++_unreadableInRow >= _photos.Count) { _advance.Stop(); CloseRequested?.Invoke(); return; }
            if (IsPlaying) Advance();
            return;
        }
        _unreadableInRow = 0;

        FileName = photo.FileName;
        var motion = _slideshow.NextMotion(Settings.SlideshowZoomIntensity, Settings.SlideshowPanIntensity);
        SlideReady?.Invoke(new SlideshowSlide(photo, image, motion));
        if (IsPlaying) _advance.Start();
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

    /// <summary>Păstrează în memorie doar poza curentă, vecinele și pe cea următoare (imagini mari).</summary>
    private void TrimCache()
    {
        var keep = new HashSet<long>();
        for (var i = Math.Max(0, Index - 1); i <= Math.Min(_photos.Count - 1, Index + 1); i++) keep.Add(_photos[i].Id);
        if (NextIndex is { } next) keep.Add(_photos[next].Id);
        foreach (var id in _loads.Keys.Where(id => !keep.Contains(id)).ToList()) _loads.Remove(id);
    }

    public void Dispose()
    {
        _advance.Stop();
        _music.Dispose();
    }
}
