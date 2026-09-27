using System.Windows;
using PhotoVault.App.ViewModels;
using PhotoVault.App.Views;
using PhotoVault.Core.Services;

namespace PhotoVault.App.Utils;

public sealed class WindowService(IMetadataService metadata, IPhotoService photoService) : IWindowService
{
    private static Window? Owner => Application.Current.MainWindow;

    public PhotoItemViewModel? ShowLightbox(IReadOnlyList<PhotoItemViewModel> photos, int startIndex)
    {
        var viewModel = new LightboxViewModel(photos, startIndex, metadata, photoService);
        var window = new LightboxWindow { DataContext = viewModel, Owner = Owner };
        window.ShowDialog();
        return viewModel.Current;
    }

    public void ShowLogo()
    {
        var window = new LogoWindow();
        window.AttachTo(Owner);
        window.ShowDialog();
    }

    public void ShowInfo()
    {
        var window = new InfoWindow();
        window.AttachTo(Owner);
        window.ShowDialog();
    }

    public void ShowSettings(MainViewModel viewModel)
    {
        var window = new SettingsWindow { DataContext = viewModel, Owner = Owner };
        window.ShowDialog();
    }
}
