using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>Panoul „Slideshow" din Opțiuni (§6.10): durate, intensități, volum și playlist. Salvare imediată.</summary>
public partial class SlideshowSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IFilePicker _filePicker;
    private readonly bool _loading;

    public SlideshowSettingsViewModel(ISettingsService settings, IFilePicker filePicker)
    {
        _settings = settings;
        _filePicker = filePicker;
        var s = settings.Current;
        _loading = true;
        DurationSec = s.SlideshowDurationSec;
        FadeSec = s.SlideshowFadeMs / 1000.0;
        PanPercent = s.SlideshowPanIntensity;
        ZoomFactor = s.SlideshowZoomIntensity;
        Volume = s.SlideshowVolume;
        _loading = false;
        LoadTracks();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    public partial double DurationSec { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FadeText))]
    public partial double FadeSec { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PanText))]
    public partial double PanPercent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomText))]
    public partial double ZoomFactor { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VolumeText))]
    public partial double Volume { get; set; }

    public string DurationText => DurationSec.ToString("0.#", Loc.Culture) + " s";
    public string FadeText => FadeSec.ToString("0.0", Loc.Culture) + " s";
    public string PanText => PanPercent.ToString("0", Loc.Culture) + "%";
    public string ZoomText => ZoomFactor.ToString("0.00", Loc.Culture) + "×";
    public string VolumeText => Volume.ToString("0", Loc.Culture) + "%";

    public ObservableCollection<MusicTrackViewModel> Tracks { get; } = [];

    [ObservableProperty]
    public partial bool HasTracks { get; set; }

    partial void OnDurationSecChanged(double value) => Save(s => s.SlideshowDurationSec = Math.Round(value * 2) / 2);
    partial void OnFadeSecChanged(double value) => Save(s => s.SlideshowFadeMs = (int)Math.Round(value * 10) * 100);
    partial void OnPanPercentChanged(double value) => Save(s => s.SlideshowPanIntensity = Math.Round(value));
    partial void OnZoomFactorChanged(double value) => Save(s => s.SlideshowZoomIntensity = Math.Round(value, 2));
    partial void OnVolumeChanged(double value) => Save(s => s.SlideshowVolume = (int)Math.Round(value));

    private void Save(Action<Core.Models.AppSettings> apply)
    {
        if (_loading) return;
        apply(_settings.Current);
        _settings.Save();
    }

    [RelayCommand]
    private void AddTracks()
    {
        var files = _filePicker.PickFiles(Loc.Get("Str.Settings.MusicPick"), Loc.Get("Str.Settings.MusicFilter") + "|*.mp3");
        if (files.Count == 0) return;
        var playlist = _settings.Current.SlideshowPlaylistPaths;
        foreach (var file in files)
            if (!playlist.Contains(file, StringComparer.OrdinalIgnoreCase)) playlist.Add(file);
        _settings.Save();
        LoadTracks();
    }

    public void RemoveTrack(MusicTrackViewModel track)
    {
        _settings.Current.SlideshowPlaylistPaths.RemoveAll(p => string.Equals(p, track.Path, StringComparison.OrdinalIgnoreCase));
        _settings.Save();
        LoadTracks();
    }

    /// <summary>Reîncarcă lista (piesele pot fi adăugate și din bara slideshow-ului).</summary>
    public void Reload() => LoadTracks();

    private void LoadTracks()
    {
        Tracks.Clear();
        foreach (var path in _settings.Current.SlideshowPlaylistPaths)
            Tracks.Add(new MusicTrackViewModel(path, File.Exists(path), this));
        HasTracks = Tracks.Count > 0;
    }
}

/// <summary>O piesă din playlist-ul slideshow-ului.</summary>
public partial class MusicTrackViewModel(string path, bool exists, SlideshowSettingsViewModel owner) : ObservableObject
{
    public string Path { get; } = path;
    public string Name => System.IO.Path.GetFileName(Path);
    public bool Exists { get; } = exists;

    [RelayCommand]
    private void Remove() => owner.RemoveTrack(this);
}
