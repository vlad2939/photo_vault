using System.Globalization;
using System.Text.Json;
using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

/// <summary>
/// Mapare între <see cref="AppSettings"/> și perechile cheie-valoare din DB.
/// Valorile lipsă sau invalide revin la valorile implicite.
/// </summary>
public sealed class SettingsService(IAppSettingsRepository repository) : ISettingsService
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public AppSettings Current { get; private set; } = new();

    public AppSettings Load()
    {
        var values = repository.GetAll();
        var defaults = new AppSettings();

        string? Raw(string key) => values.TryGetValue(key, out var v) ? v : null;

        double Double(string key, double fallback, double min, double max) =>
            double.TryParse(Raw(key), NumberStyles.Float, Inv, out var d) ? Math.Clamp(d, min, max) : fallback;

        Current = new AppSettings
        {
            Theme = string.Equals(Raw(AppSettingKeys.Theme), "light", StringComparison.OrdinalIgnoreCase)
                ? AppTheme.Light
                : AppTheme.Dark,
            AccentColor = IsHexColor(Raw(AppSettingKeys.AccentColor)) ? Raw(AppSettingKeys.AccentColor)! : defaults.AccentColor,
            Language = Raw(AppSettingKeys.Language) is "en" ? "en" : "ro",
            SlideshowDurationSec = Double(AppSettingKeys.SlideshowDurationSec, defaults.SlideshowDurationSec, 3, 15),
            SlideshowFadeMs = (int)Double(AppSettingKeys.SlideshowFadeMs, defaults.SlideshowFadeMs, 500, 3000),
            SlideshowPanIntensity = Double(AppSettingKeys.SlideshowPanIntensity, defaults.SlideshowPanIntensity, 5, 25),
            SlideshowZoomIntensity = Double(AppSettingKeys.SlideshowZoomIntensity, defaults.SlideshowZoomIntensity, 1.05, 1.30),
            SlideshowPlaylistPaths = ParsePlaylist(Raw(AppSettingKeys.SlideshowPlaylistPaths)),
            SlideshowVolume = (int)Double(AppSettingKeys.SlideshowVolume, defaults.SlideshowVolume, 0, 100),
            SlideshowLoop = bool.TryParse(Raw(AppSettingKeys.SlideshowLoop), out var loop) ? loop : defaults.SlideshowLoop,
            GridThumbnailSize = Double(AppSettingKeys.GridThumbnailSize, defaults.GridThumbnailSize,
                AppSettings.MinGridThumbnailSize, AppSettings.MaxGridThumbnailSize),
        };
        return Current;
    }

    public void Save()
    {
        var s = Current;
        repository.Set(AppSettingKeys.Theme, s.Theme == AppTheme.Light ? "light" : "dark");
        repository.Set(AppSettingKeys.AccentColor, s.AccentColor);
        repository.Set(AppSettingKeys.Language, s.Language);
        repository.Set(AppSettingKeys.SlideshowDurationSec, s.SlideshowDurationSec.ToString(Inv));
        repository.Set(AppSettingKeys.SlideshowFadeMs, s.SlideshowFadeMs.ToString(Inv));
        repository.Set(AppSettingKeys.SlideshowPanIntensity, s.SlideshowPanIntensity.ToString(Inv));
        repository.Set(AppSettingKeys.SlideshowZoomIntensity, s.SlideshowZoomIntensity.ToString(Inv));
        repository.Set(AppSettingKeys.SlideshowPlaylistPaths, JsonSerializer.Serialize(s.SlideshowPlaylistPaths));
        repository.Set(AppSettingKeys.SlideshowVolume, s.SlideshowVolume.ToString(Inv));
        repository.Set(AppSettingKeys.SlideshowLoop, s.SlideshowLoop ? "true" : "false");
        repository.Set(AppSettingKeys.GridThumbnailSize, s.GridThumbnailSize.ToString(Inv));
    }

    public void SetTheme(AppTheme theme)
    {
        Current.Theme = theme;
        repository.Set(AppSettingKeys.Theme, theme == AppTheme.Light ? "light" : "dark");
    }

    public void SetAccentColor(string hex)
    {
        if (!IsHexColor(hex)) return;
        Current.AccentColor = hex.ToUpperInvariant();
        repository.Set(AppSettingKeys.AccentColor, Current.AccentColor);
    }

    public void SetGridThumbnailSize(double size)
    {
        Current.GridThumbnailSize = Math.Clamp(Math.Round(size), AppSettings.MinGridThumbnailSize, AppSettings.MaxGridThumbnailSize);
        repository.Set(AppSettingKeys.GridThumbnailSize, Current.GridThumbnailSize.ToString(Inv));
    }

    public void SetLanguage(string language)
    {
        Current.Language = language == "en" ? "en" : "ro";
        repository.Set(AppSettingKeys.Language, Current.Language);
    }

    private static bool IsHexColor(string? value) =>
        value is { Length: 7 } && value[0] == '#' &&
        int.TryParse(value.AsSpan(1), NumberStyles.HexNumber, Inv, out _);

    private static List<string> ParsePlaylist(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
