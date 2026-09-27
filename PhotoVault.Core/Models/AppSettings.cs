namespace PhotoVault.Core.Models;

/// <summary>
/// Setările aplicației, în formă tipizată. Persistate ca perechi cheie-valoare
/// în tabela AppSettings (vezi <see cref="AppSettingKeys"/>).
/// </summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;

    /// <summary>Culoarea de accent, cod hex (#RRGGBB).</summary>
    public string AccentColor { get; set; } = DefaultAccentColor;

    /// <summary>Limba interfeței: "ro" sau "en".</summary>
    public string Language { get; set; } = "ro";

    public double SlideshowDurationSec { get; set; } = 6;
    public int SlideshowFadeMs { get; set; } = 1200;

    /// <summary>Intensitate pan, procent din dimensiunea imaginii (5–25).</summary>
    public double SlideshowPanIntensity { get; set; } = 10;

    /// <summary>Factor maxim de scalare (1.05–1.30).</summary>
    public double SlideshowZoomIntensity { get; set; } = 1.15;

    public List<string> SlideshowPlaylistPaths { get; set; } = [];

    /// <summary>Lățimea minimă a cardurilor din grid, în pixeli (§12.4: 120 = mic … 320 = mare).</summary>
    public double GridThumbnailSize { get; set; } = DefaultGridThumbnailSize;

    public const double DefaultGridThumbnailSize = 170;
    public const double MinGridThumbnailSize = 120;
    public const double MaxGridThumbnailSize = 320;

    /// <summary>Volumul muzicii din slideshow, 0–100.</summary>
    public int SlideshowVolume { get; set; } = 70;

    public const string DefaultAccentColor = "#F2821A";

    /// <summary>Paleta de accent predefinită (§6.12): portocaliu (implicit, ca în mockup-uri), albastru, verde, roșu, mov, turcoaz.</summary>
    public static IReadOnlyList<string> AccentPresets { get; } =
        ["#F2821A", "#2D7FF9", "#2EAD5B", "#E5484D", "#8E5CF7", "#14B8A6"];

    /// <summary>Limbile interfeței (§6.12).</summary>
    public static IReadOnlyList<string> Languages { get; } = ["ro", "en"];
}

/// <summary>Cheile folosite în tabela AppSettings.</summary>
public static class AppSettingKeys
{
    public const string Theme = "Theme";
    public const string AccentColor = "AccentColor";
    public const string Language = "Language";
    public const string SlideshowDurationSec = "SlideshowDurationSec";
    public const string SlideshowFadeMs = "SlideshowFadeMs";
    public const string SlideshowPanIntensity = "SlideshowPanIntensity";
    public const string SlideshowZoomIntensity = "SlideshowZoomIntensity";
    public const string SlideshowPlaylistPaths = "SlideshowPlaylistPaths";
    public const string SlideshowVolume = "SlideshowVolume";
    public const string GridThumbnailSize = "GridThumbnailSize";
}
