using System.ComponentModel;
using System.Windows;
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
            if (ViewModel is { } vm) await vm.InitializeAsync();
        };
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    /// <summary>TreeView.SelectedItem nu e bindabil → selecția e transmisă explicit ViewModel-ului.</summary>
    private void OnFolderSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (ViewModel is { } vm && e.NewValue is FolderNodeViewModel node) vm.Library.SelectedFolder = node;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        ViewModel?.Shutdown();
        base.OnClosing(e);
    }
}
