using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// ViewModel-ul ferestrei principale: compune panourile (Bibliotecă, grid, detalii, footer)
/// și comenzile barei secundare (§5.3).
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IDialogService _dialogs;
    private readonly IWindowService _windows;
    private readonly CancellationTokenSource _shutdown = new();

    public MainViewModel(ISettingsService settings, IThemeService theme, IDialogService dialogs, IWindowService windows,
        IPhotoIndexService index, IThumbnailService thumbnails, IMetadataService metadata, IFolderPicker folderPicker)
    {
        _settings = settings;
        _theme = theme;
        _dialogs = dialogs;
        _windows = windows;
        IsDarkTheme = theme.CurrentTheme == AppTheme.Dark;

        Status = new StatusBarViewModel();
        Grid = new PhotoGridViewModel(thumbnails, windows);
        Details = new PhotoDetailsViewModel(metadata);
        Library = new FolderTreeViewModel(index, dialogs, folderPicker, Status, Grid, _shutdown.Token);

        Grid.PropertyChanged += OnGridPropertyChanged;
    }

    /// <summary>Footer: progres indexare / mesaje de stare.</summary>
    public StatusBarViewModel Status { get; }

    /// <summary>Grid-ul central de miniaturi.</summary>
    public PhotoGridViewModel Grid { get; }

    /// <summary>Panoul lateral de detalii.</summary>
    public PhotoDetailsViewModel Details { get; }

    /// <summary>Secțiunea Bibliotecă (arbore foldere sursă).</summary>
    public FolderTreeViewModel Library { get; }

    /// <summary>Tema curentă; comută iconița butonului (lună = dark, soare = light).</summary>
    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    /// <summary>Apelat după afișarea ferestrei.</summary>
    public Task InitializeAsync() => Library.InitializeAsync();

    /// <summary>Oprește operațiunile de fundal la închiderea aplicației.</summary>
    public void Shutdown() => _shutdown.Cancel();

    [RelayCommand]
    private void ToggleTheme() => SetTheme(IsDarkTheme ? AppTheme.Light : AppTheme.Dark);

    /// <summary>Selectorul de temă din Setări.</summary>
    [RelayCommand]
    private void SetTheme(AppTheme theme)
    {
        if ((theme == AppTheme.Dark) == IsDarkTheme) return;
        _theme.SetTheme(theme);
        _settings.SetTheme(theme);
        IsDarkTheme = theme == AppTheme.Dark;
    }

    [RelayCommand]
    private void OpenBatchRename() =>
        // Modulul de redenumire batch se implementează în Faza 5 (§6.8)
        _dialogs.Show(Loc.Get("Str.Placeholder.Title"), Loc.Get("Str.Placeholder.BatchRename"), DialogKind.Info);

    [RelayCommand]
    private void OpenInfo() => _windows.ShowInfo();

    [RelayCommand]
    private void OpenSettings() => _windows.ShowSettings(this);

    [RelayCommand]
    private void OpenLogo() => _windows.ShowLogo();

    private async void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PhotoGridViewModel.SelectedPhoto))
            await Details.ShowAsync(Grid.SelectedPhoto);
    }
}
