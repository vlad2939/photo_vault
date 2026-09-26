using System.Windows;
using System.Windows.Media;
using PhotoVault.App.Controls;
using PhotoVault.Core.Models;

namespace PhotoVault.App.Utils;

/// <summary>
/// Gestionează tema (Dark/Light) și culoarea de accent (§6.11, §6.12).
/// Swap-ul se face în Application.Resources.MergedDictionaries, iar toate
/// controalele folosesc DynamicResource → schimbarea e instantă, fără repornire.
/// </summary>
public sealed class ThemeManager : IThemeService
{
    private const string ThemesFolder = "/Themes/";

    /// <summary>Paleta de accent predefinită (6 culori, §6.12 — selector în Faza 7).</summary>
    public static IReadOnlyList<string> AccentPresets { get; } =
    [
        "#F2821A", // portocaliu (implicit, conform mockup-urilor)
        "#2D7FF9", // albastru
        "#2EAD5B", // verde
        "#E5484D", // roșu
        "#8E5CF7", // mov
        "#14B8A6", // turcoaz
    ];

    private ResourceDictionary? _accentDictionary;

    public static ThemeManager Instance { get; } = new();

    private ThemeManager() { }

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;
    public Color AccentColor { get; private set; } = ParseColor(AppSettings.DefaultAccentColor);

    public event EventHandler? ThemeChanged;

    /// <summary>Aplicare inițială, la pornire (înainte de afișarea ferestrelor).</summary>
    public void Initialize(AppTheme theme, string accentHex)
    {
        AccentColor = ParseColor(accentHex);
        ApplyTheme(theme);
    }

    public void SetTheme(AppTheme theme)
    {
        if (theme == CurrentTheme && _accentDictionary is not null) return;
        ApplyTheme(theme);
    }

    public void SetAccent(string accentHex)
    {
        AccentColor = ParseColor(accentHex);
        ApplyAccent();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        var file = theme == AppTheme.Light ? "LightTheme.xaml" : "DarkTheme.xaml";
        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/PhotoVault;component/Themes/{file}", UriKind.Absolute)
        };

        var merged = Application.Current.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d => d.Source?.OriginalString.Contains(ThemesFolder, StringComparison.OrdinalIgnoreCase) == true);
        if (existing is null)
            merged.Insert(0, dictionary);
        else
            merged[merged.IndexOf(existing)] = dictionary;

        // Accentul depinde de fundal (variante hover/subtle diferite), deci se regenerează.
        ApplyAccent();
        UpdateAllTitleBars();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Generează resursele de accent adaptate temei active: accentul rămâne
    /// aceeași nuanță, dar hover/pressed/subtle se ajustează la fundal.
    /// </summary>
    private void ApplyAccent()
    {
        var accent = AccentColor;
        var isDark = CurrentTheme == AppTheme.Dark;

        var hover = isDark ? Mix(accent, Colors.White, 0.12) : Mix(accent, Colors.Black, 0.08);
        var pressed = isDark ? Mix(accent, Colors.Black, 0.12) : Mix(accent, Colors.Black, 0.18);
        var subtle = Color.FromArgb(isDark ? (byte)0x38 : (byte)0x26, accent.R, accent.G, accent.B);
        var text = isDark ? Mix(accent, Colors.White, 0.10) : Mix(accent, Colors.Black, 0.15);
        var foreground = RelativeLuminance(accent) > 0.55 ? Color.FromRgb(0x1A, 0x1A, 0x1A) : Colors.White;

        var dictionary = new ResourceDictionary
        {
            ["Color.Accent"] = accent,
            ["Brush.Accent"] = Frozen(accent),
            ["Brush.Accent.Hover"] = Frozen(hover),
            ["Brush.Accent.Pressed"] = Frozen(pressed),
            ["Brush.Accent.Subtle"] = Frozen(subtle),
            ["Brush.Accent.Text"] = Frozen(text),
            ["Brush.Accent.Foreground"] = Frozen(foreground),
        };

        var merged = Application.Current.Resources.MergedDictionaries;
        if (_accentDictionary is not null && merged.Contains(_accentDictionary))
            merged[merged.IndexOf(_accentDictionary)] = dictionary;
        else
            merged.Insert(Math.Min(1, merged.Count), dictionary);
        _accentDictionary = dictionary;
    }

    /// <summary>Aplică tema pe title bar-ul nativ al unei ferestre (apelat la SourceInitialized).</summary>
    public void UpdateTitleBar(Window window)
    {
        var resources = Application.Current.Resources;
        var caption = resources["Color.TitleBar"] is Color c ? c : Colors.Black;
        var text = resources["Color.TitleBarText"] is Color t ? t : Colors.White;
        DwmTitleBar.Apply(window, CurrentTheme == AppTheme.Dark, caption, text);
    }

    private void UpdateAllTitleBars()
    {
        foreach (var window in Application.Current.Windows.OfType<ThemedWindow>())
            UpdateTitleBar(window);
    }

    public static Color ParseColor(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch (FormatException)
        {
            return (Color)ColorConverter.ConvertFromString(AppSettings.DefaultAccentColor);
        }
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color Mix(Color a, Color b, double amount) => Color.FromArgb(
        a.A,
        (byte)Math.Round(a.R + (b.R - a.R) * amount),
        (byte)Math.Round(a.G + (b.G - a.G) * amount),
        (byte)Math.Round(a.B + (b.B - a.B) * amount));

    private static double RelativeLuminance(Color c)
    {
        static double Channel(byte v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }
}
