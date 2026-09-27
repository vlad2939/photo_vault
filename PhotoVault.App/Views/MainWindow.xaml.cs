using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

    /// <summary>
    /// Dublu-click pe un card → lightbox. Tratat în faza „Preview", pentru că ListBoxItem
    /// marchează click-ul ca procesat (selecție) și un MouseBinding pe ListBox nu l-ar mai primi.
    /// </summary>
    private void OnPhotoListMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || ViewModel is not { } vm) return;
        var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (item?.DataContext is not PhotoItemViewModel photo) return;

        e.Handled = true;
        vm.Grid.SelectedPhoto = photo;
        vm.Grid.OpenCommand.Execute(photo);
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null and not T)
            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        return element as T;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        ViewModel?.Shutdown();
        base.OnClosing(e);
    }
}
