using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// ViewModel-ul ferestrei principale: compune panourile (Bibliotecă, grid, footer)
/// și comenzile barei secundare (§5.3).
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IDialogService _dialogs;
    private readonly CancellationTokenSource _shutdown = new();

    public MainViewModel(ISettingsService settings, IThemeService theme, IDialogService dialogs,
        IPhotoIndexService index, IThumbnailService thumbnails, IFolderPicker folderPicker)
    {
        _settings = settings;
        _theme = theme;
        _dialogs = dialogs;
        IsDarkTheme = theme.CurrentTheme == AppTheme.Dark;

        Status = new StatusBarViewModel();
        Grid = new PhotoGridViewModel(thumbnails);
        Library = new FolderTreeViewModel(index, dialogs, folderPicker, Status, Grid, _shutdown.Token);
    }

    /// <summary>Footer: progres indexare / mesaje de stare.</summary>
    public StatusBarViewModel Status { get; }

    /// <summary>Grid-ul central de miniaturi.</summary>
    public PhotoGridViewModel Grid { get; }

    /// <summary>Secțiunea Bibliotecă (foldere sursă).</summary>
    public FolderTreeViewModel Library { get; }

    /// <summary>Tema curentă; comută iconița butonului (lună = dark, soare = light).</summary>
    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    /// <summary>Apelat după afișarea ferestrei.</summary>
    public Task InitializeAsync() => Library.InitializeAsync();

    /// <summary>Oprește operațiunile de fundal la închiderea aplicației.</summary>
    public void Shutdown() => _shutdown.Cancel();

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
