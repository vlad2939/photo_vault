using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Secțiunea „General" din Opțiuni (§6.12): culoarea de accent (6 buline, aplicată imediat)
/// și limba interfeței (salvată imediat, aplicată la repornire — cu opțiunea de repornire pe loc).
/// </summary>
public partial class GeneralSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IDialogService _dialogs;
    private readonly IAppLifetime _lifetime;

    /// <summary>Limba cu care rulează acum aplicația (textele sunt încărcate la pornire).</summary>
    private readonly string _activeLanguage;

    public GeneralSettingsViewModel(ISettingsService settings, IThemeService theme, IDialogService dialogs, IAppLifetime lifetime)
    {
        _settings = settings;
        _theme = theme;
        _dialogs = dialogs;
        _lifetime = lifetime;
        _activeLanguage = settings.Current.Language;

        string[] names = ["Orange", "Blue", "Green", "Red", "Purple", "Teal"];
        Accents = AppSettings.AccentPresets
            .Select((hex, i) => new AccentOptionViewModel(hex, $"Str.Accent.{names[i]}", this))
            .ToList();
        UpdateAccentSelection();
    }

    public IReadOnlyList<AccentOptionViewModel> Accents { get; }

    public bool IsRomanian => _settings.Current.Language == "ro";
    public bool IsEnglish => _settings.Current.Language == "en";

    /// <summary>Limba aleasă diferă de cea activă → mesaj discret „se aplică după repornire".</summary>
    public bool IsRestartPending => _settings.Current.Language != _activeLanguage;

    public void SelectAccent(AccentOptionViewModel accent)
    {
        _settings.SetAccentColor(accent.Hex);
        _theme.SetAccent(accent.Hex);
        UpdateAccentSelection();
    }

    private void UpdateAccentSelection()
    {
        foreach (var accent in Accents)
            accent.IsSelected = string.Equals(accent.Hex, _settings.Current.AccentColor, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void SetLanguage(string language)
    {
        if (language == _settings.Current.Language) return;
        _settings.SetLanguage(language);
        OnPropertyChanged(nameof(IsRomanian));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(IsRestartPending));
        if (!IsRestartPending) return;

        // Textele noi se încarcă doar la pornire → mesajul apare încă în limba curentă
        var answer = _dialogs.Show(Loc.Get("Str.Settings.LanguageTitle"), Loc.Get("Str.Settings.LanguageRestart"),
            DialogKind.Info, DialogButtons.YesNo,
            primaryText: Loc.Get("Str.Settings.RestartNow"), secondaryText: Loc.Get("Str.Settings.RestartLater"));
        if (answer == DialogResultKind.Yes) _lifetime.Restart();
    }
}

/// <summary>O bulină din selectorul de accent.</summary>
public partial class AccentOptionViewModel(string hex, string nameKey, GeneralSettingsViewModel owner) : ObservableObject
{
    public string Hex { get; } = hex;
    public string Name => Loc.Get(nameKey);
    public Brush Brush { get; } = CreateBrush(hex);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [RelayCommand]
    private void Select() => owner.SelectAccent(this);

    private static SolidColorBrush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush(ThemeManager.ParseColor(hex));
        brush.Freeze();
        return brush;
    }
}
