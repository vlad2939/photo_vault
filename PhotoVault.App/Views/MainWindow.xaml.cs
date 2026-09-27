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

    /// <summary>Selecția multiplă din grid (ListBox.SelectedItems nu e bindabil).</summary>
    private void OnPhotoSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ViewModel?.Grid.UpdateSelection(PhotoList.SelectedItems.OfType<PhotoItemViewModel>());

    /// <summary>Dublu-click pe un card de album → deschide albumul; un click → detalii (prin selecție).</summary>
    private void OnAlbumCardsMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject)?.DataContext is not AlbumItemViewModel album) return;
        e.Handled = true;
        album.OpenCommand.Execute(null);
    }

    /// <summary>
    /// Meniul contextual al pozelor (§6.1) e construit la deschidere, pentru că listele de albume
    /// și tag-uri sunt dinamice, iar unele opțiuni depind de context (album deschis, selecție unică).
    /// </summary>
    private void OnPhotoContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (ViewModel is not { } vm || PhotoList.ContextMenu is not { } menu) return;

        // Click dreapta în afara unei poze → fără meniu (din tastatură — Shift+F10 / tasta Meniu — e suficientă o selecție)
        var fromKeyboard = e.CursorLeft < 0 && e.CursorTop < 0;
        if (vm.Grid.SelectedPhotos.Count == 0 ||
            (!fromKeyboard && FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is null))
        {
            e.Handled = true;
            return;
        }

        menu.Items.Clear();
        menu.Items.Add(MenuItem("Str.Photo.Open", vm.Grid.OpenCommand, vm.Grid.SelectedPhoto));
        var rotate = MenuItem("Str.Photo.Rotate", vm.RotateCommand, null, "Icon.Rotate");
        rotate.InputGestureText = "R";
        menu.Items.Add(rotate);
        var slideshow = MenuItem("Str.Slideshow.StartHere", vm.StartSlideshowCommand, vm.Grid.SelectedPhoto, "Icon.Slideshow");
        slideshow.InputGestureText = "F5";
        menu.Items.Add(slideshow);
        menu.Items.Add(new Separator());

        var albumMenu = MenuItem("Str.Photo.AddToAlbum", null, null, "Icon.Album");
        albumMenu.Items.Add(MenuItem("Str.Albums.NewEllipsis", vm.AddToAlbumCommand, null, "Icon.Add"));
        if (vm.Albums.Albums.Count > 0) albumMenu.Items.Add(new Separator());
        foreach (var album in vm.Albums.Albums)
            albumMenu.Items.Add(new MenuItem { Header = album.Name, Command = vm.AddToAlbumCommand, CommandParameter = album });
        menu.Items.Add(albumMenu);

        var tagMenu = MenuItem("Str.Photo.AddTag", null, null, "Icon.Tag");
        tagMenu.Items.Add(MenuItem("Str.Tags.NewEllipsis", vm.AddTagCommand, null, "Icon.Add"));
        if (vm.Tags.Tags.Count > 0) tagMenu.Items.Add(new Separator());
        foreach (var tag in vm.Tags.Tags)
            tagMenu.Items.Add(new MenuItem { Header = tag.Name, Command = vm.AddTagCommand, CommandParameter = tag });
        menu.Items.Add(tagMenu);

        if (vm.IsAlbumContext)
        {
            menu.Items.Add(new Separator());
            if (vm.Grid.SelectedPhotos.Count == 1)
                menu.Items.Add(MenuItem("Str.Photo.SetCover", vm.SetAlbumCoverCommand, null, "Icon.Photo"));
            menu.Items.Add(MenuItem("Str.Photo.RemoveFromAlbum", vm.RemoveFromAlbumCommand, null, "Icon.Delete"));
        }
    }

    /// <summary>„+" la Tag-uri în panoul de detalii: tag-urile existente (neatribuite pozei) + „Tag nou…".</summary>
    private void OnAddTagClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;
        var assigned = vm.Details.Tags.Select(t => t.Id).ToHashSet();

        var menu = new ContextMenu { PlacementTarget = AddTagButton, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        menu.Items.Add(MenuItem("Str.Tags.NewEllipsis", vm.AddTagToCurrentPhotoCommand, null, "Icon.Add"));
        var available = vm.Tags.Tags.Where(t => !assigned.Contains(t.Id)).ToList();
        if (available.Count > 0) menu.Items.Add(new Separator());
        foreach (var tag in available)
            menu.Items.Add(new MenuItem { Header = tag.Name, Command = vm.AddTagToCurrentPhotoCommand, CommandParameter = tag });
        menu.IsOpen = true;
    }

    private MenuItem MenuItem(string headerKey, ICommand? command, object? parameter, string? iconKey = null) => new()
    {
        Header = TryFindResource(headerKey) as string ?? headerKey,
        Command = command,
        CommandParameter = parameter,
        Icon = iconKey is null ? null : TryFindResource(iconKey) as string,
    };

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null and not T)
            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        return element as T;
    }

    /// <summary>Ctrl+F: focus pe câmpul de căutare (§5.3).</summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None && ViewModel is { } vm)
        {
            vm.StartSlideshowCommand.Execute(null);
            e.Handled = true;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        ViewModel?.Shutdown();
        base.OnClosing(e);
    }
}
