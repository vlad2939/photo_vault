using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// ViewModel-ul ferestrei principale: bara secundară (§5.3) și footer-ul (§5.5).
/// Faza 0: doar comutarea temei este funcțională; celelalte butoane afișează
/// un mesaj informativ până la implementarea modulelor lor.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IDialogService _dialogs;

    public MainViewModel(ISettingsService settings, IThemeService theme, IDialogService dialogs)
    {
        _settings = settings;
        _theme = theme;
        _dialogs = dialogs;
        IsDarkTheme = theme.CurrentTheme == AppTheme.Dark;
    }

    /// <summary>Tema curentă; comută iconița butonului (lună = dark, soare = light).</summary>
    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    // ---- Footer: zona de stare / progres (populată din Faza 1, la indexare) ----

    [ObservableProperty]
    public partial string? StatusText { get; set; }

    [ObservableProperty]
    public partial bool IsProgressVisible { get; set; }

    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    [ObservableProperty]
    public partial double ProgressMaximum { get; set; } = 100;

    [RelayCommand]
    private void ToggleTheme()
    {
        var next = IsDarkTheme ? AppTheme.Light : AppTheme.Dark;
        _theme.SetTheme(next);
        _settings.SetTheme(next);
        IsDarkTheme = next == AppTheme.Dark;
    }

    [RelayCommand]
    private void OpenBatchRename() => ShowComingSoon();

    [RelayCommand]
    private void OpenInfo() => ShowComingSoon();

    [RelayCommand]
    private void OpenSettings() => ShowComingSoon();

    [RelayCommand]
    private void OpenLogo()
    {
        // Faza 2: modal fullscreen cu logo-ul peste fundal blurat (§5.4).
    }

    private void ShowComingSoon() =>
        _dialogs.Show(Loc.Get("Str.Placeholder.Title"), Loc.Get("Str.Placeholder.Message"), DialogKind.Info);
}
