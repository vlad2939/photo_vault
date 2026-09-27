using System.Windows;
using System.Windows.Input;
using PhotoVault.App.ViewModels;

namespace PhotoVault.App.Views;

public partial class LightboxWindow : Window
{
    public LightboxWindow()
    {
        InitializeComponent();
    }

    private LightboxViewModel? ViewModel => DataContext as LightboxViewModel;

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        switch (e.Key)
        {
            case Key.Escape: Close(); break;
            case Key.Right or Key.PageDown: ViewModel?.NextCommand.Execute(null); break;
            case Key.Left or Key.PageUp: ViewModel?.PreviousCommand.Execute(null); break;
            case Key.Home: ViewModel?.GoTo(0); break;
            case Key.End when ViewModel is { } vm: vm.GoTo(vm.Count - 1); break;
            case Key.Add or Key.OemPlus: Viewer.ZoomIn(); break;
            case Key.Subtract or Key.OemMinus: Viewer.ZoomOut(); break;
            case Key.D0 or Key.NumPad0: Viewer.Fit(); break;
            case Key.D1 or Key.NumPad1: Viewer.ActualSize(); break;
            default: return;
        }
        e.Handled = true;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    private void OnZoomInClick(object sender, RoutedEventArgs e) => Viewer.ZoomIn();
    private void OnZoomOutClick(object sender, RoutedEventArgs e) => Viewer.ZoomOut();
    private void OnFitClick(object sender, RoutedEventArgs e) => Viewer.Fit();
    private void OnActualSizeClick(object sender, RoutedEventArgs e) => Viewer.ActualSize();
}
