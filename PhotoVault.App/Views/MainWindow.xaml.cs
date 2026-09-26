using System.ComponentModel;
using PhotoVault.App.Controls;
using PhotoVault.App.ViewModels;

namespace PhotoVault.App.Views;

public partial class MainWindow : ThemedWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is MainViewModel vm) await vm.InitializeAsync();
        };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        (DataContext as MainViewModel)?.Shutdown();
        base.OnClosing(e);
    }
}
