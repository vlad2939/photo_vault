using System.ComponentModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>Ce afișează zona centrală.</summary>
public enum BrowseContext
{
    AllPhotos,
    Folder,
    /// <summary>Grid-ul de carduri de albume (§5.7).</summary>
    Albums,
    Album,
    Tag
}

/// <summary>Ce afișează panoul de detalii (§5.6).</summary>
public enum DetailsMode
{
    None,
    Photo,
    Album
}

/// <summary>
/// ViewModel-ul ferestrei principale: compune panourile (Bibliotecă, albume, tag-uri, grid, detalii,
/// footer), coordonează contextul de navigare și acțiunile pe pozele selectate (albume, tag-uri).
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IDialogService _dialogs;
    private readonly IWindowService _windows;
    private readonly IAlbumService _albumService;
    private readonly ITagService _tagService;
    private readonly IPhotoService _photoService;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private bool _navigating;

    public MainViewModel(ISettingsService settings, IThemeService theme, IDialogService dialogs, IWindowService windows,
        IPhotoIndexService index, IThumbnailService thumbnails, IMetadataService metadata, IAlbumService albumService,
        ITagService tagService, IPhotoService photoService, IFolderPicker folderPicker, IFilePicker filePicker)
    {
        _settings = settings;
        _theme = theme;
        _dialogs = dialogs;
        _windows = windows;
        _albumService = albumService;
        _tagService = tagService;
        _photoService = photoService;
        IsDarkTheme = theme.CurrentTheme == AppTheme.Dark;

        Status = new StatusBarViewModel();
        Grid = new PhotoGridViewModel(thumbnails, windows);
        Details = new PhotoDetailsViewModel(metadata, tagService);
        Library = new FolderTreeViewModel(index, dialogs, folderPicker, Status, Grid, _shutdown.Token);
        Albums = new AlbumViewModel(albumService, thumbnails, dialogs);
        Tags = new TagViewModel(tagService, dialogs);
        SlideshowSettings = new SlideshowSettingsViewModel(settings, filePicker);

        Grid.PropertyChanged += OnGridPropertyChanged;
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            Grid.SetSearch(SearchText);
        };
        Albums.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AlbumViewModel.SelectedAlbum)) OnPropertyChanged(nameof(DetailsMode));
        };
        Library.SelectedFolderChanged += OnFolderSelected;
        Library.ShowAllRequested += ShowAllPhotos;
        Library.LibraryReloaded += OnLibraryReloaded;
        Albums.OpenRequested += OpenAlbum;
        Albums.AlbumChanged += OnAlbumChanged;
        Tags.FilterRequested += ShowTag;
        Tags.TagChanged += OnTagChanged;
        Details.TagsModified += OnTagsAssignmentChanged;
    }

    public StatusBarViewModel Status { get; }
    public PhotoGridViewModel Grid { get; }
    public PhotoDetailsViewModel Details { get; }
    public FolderTreeViewModel Library { get; }
    public AlbumViewModel Albums { get; }
    public TagViewModel Tags { get; }
    public SlideshowSettingsViewModel SlideshowSettings { get; }

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    // ------------------------------------------------------------------ Context de navigare

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAlbumsView), nameof(IsPhotosView), nameof(IsAlbumContext), nameof(DetailsMode), nameof(ContextIcon))]
    public partial BrowseContext Context { get; set; } = BrowseContext.AllPhotos;

    /// <summary>Titlul contextului curent (bara de deasupra grid-ului).</summary>
    [ObservableProperty]
    public partial string ContextTitle { get; set; } = string.Empty;

    /// <summary>Albumul deschis (în contextul Album).</summary>
    [ObservableProperty]
    public partial AlbumItemViewModel? CurrentAlbum { get; set; }

    public bool IsAlbumsView => Context == BrowseContext.Albums;
    public bool IsPhotosView => Context != BrowseContext.Albums;
    public bool IsAlbumContext => Context == BrowseContext.Album;

    public string ContextIcon => Loc.Get(Context switch
    {
        BrowseContext.Folder => "Icon.Folder",
        BrowseContext.Albums or BrowseContext.Album => "Icon.Album",
        BrowseContext.Tag => "Icon.Tag",
        _ => "Icon.Library",
    });

    public DetailsMode DetailsMode => IsAlbumsView
        ? Albums.SelectedAlbum is null ? DetailsMode.None : DetailsMode.Album
        : Details.HasPhoto ? DetailsMode.Photo : DetailsMode.None;

    /// <summary>Apelat după afișarea ferestrei.</summary>
    public async Task InitializeAsync()
    {
        ContextTitle = Loc.Get("Str.Context.AllPhotos");
        Albums.Reload();
        Tags.Reload();
        await Library.InitializeAsync();
    }

    public void Shutdown() => _shutdown.Cancel();

    private void Navigate(BrowseContext context, string title, Func<PhotoItemViewModel, bool>? filter,
        AlbumItemViewModel? album = null, bool keepTree = false, bool keepTag = false)
    {
        _navigating = true;
        try
        {
            if (!keepTree) Library.ClearSelection();
            if (!keepTag) Tags.ActiveTag = null;
            Albums.ActiveAlbum = album;
            CurrentAlbum = album;
            Context = context;
            ContextTitle = title;
            if (context != BrowseContext.Albums) Grid.SetFilter(filter);
        }
        finally
        {
            _navigating = false;
        }
    }

    [RelayCommand]
    private void ShowAllPhotos() => Navigate(BrowseContext.AllPhotos, Loc.Get("Str.Context.AllPhotos"), null);

    private void OnFolderSelected(FolderNodeViewModel? node)
    {
        if (_navigating) return;
        if (node is null) ShowAllPhotos();
        else Navigate(BrowseContext.Folder, node.Name, PhotoGridViewModel.FolderFilter(node.FullPath), keepTree: true);
    }

    /// <summary>Click pe titlul „Albume": grid-ul de carduri de albume.</summary>
    [RelayCommand]
    private void ShowAlbums()
    {
        Albums.Reload();
        Navigate(BrowseContext.Albums, Loc.Get("Str.Albums"), null);
    }

    private void OpenAlbum(AlbumItemViewModel album)
    {
        if (_navigating) return;
        var ids = _albumService.GetPhotoIds(album.Id);
        Navigate(BrowseContext.Album, album.Name, PhotoGridViewModel.IdFilter(ids), album);
    }

    private void ShowTag(TagItemViewModel tag)
    {
        if (_navigating) return;
        var ids = _tagService.GetPhotoIds(tag.Id);
        Navigate(BrowseContext.Tag, "#" + tag.Name, PhotoGridViewModel.IdFilter(ids), keepTag: true);
    }

    /// <summary>Reaplică contextul curent (după modificări în albume / tag-uri / index).</summary>
    private void RefreshContext()
    {
        switch (Context)
        {
            case BrowseContext.Album when CurrentAlbum is not null && Albums.Find(CurrentAlbum.Id) is { } album:
                OpenAlbum(album);
                break;
            case BrowseContext.Album:
                ShowAlbums();   // albumul deschis a fost șters
                break;
            case BrowseContext.Tag when Tags.ActiveTag is { } tag:
                ShowTag(tag);
                break;
            case BrowseContext.Tag:
                ShowAllPhotos();   // tag-ul a fost șters
                break;
        }
    }

    /// <summary>
    /// Badge-urile de tag și cuvintele-cheie de căutare (tag-uri + albume) ale pozelor,
    /// după orice schimbare a atribuirilor / numelor.
    /// </summary>
    private void RefreshTagBadges()
    {
        Grid.SetTaggedPhotos(_tagService.GetTaggedPhotoIds());
        Grid.SetSearchKeywords(_photoService.GetSearchKeywords());
    }

    // ------------------------------------------------------------------ Căutare, sortare, rotire (§6.7, §6.9)

    /// <summary>Textul din câmpul de căutare; grid-ul se filtrează la 300 ms după ultima tastă.</summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        _searchDebounce.Stop();
        Grid.SetSearch(null);
    }

    /// <summary>0 = nume A → Z, 1 = nume Z → A (dropdown-ul de sortare).</summary>
    [ObservableProperty]
    public partial int SortIndex { get; set; }

    partial void OnSortIndexChanged(int value) => Grid.SortDescending = value == 1;

    /// <summary>R / „Rotește 90°": rotire logică a pozelor selectate; fișierele originale rămân neatinse.</summary>
    [RelayCommand]
    private void Rotate()
    {
        var targets = Grid.SelectedPhotos.Count > 0 ? Grid.SelectedPhotos
            : Grid.SelectedPhoto is { } single ? [single] : [];
        if (targets.Count == 0) return;

        var rotations = _photoService.RotateClockwise(targets.Select(p => p.Id).ToList());
        foreach (var photo in targets)
            if (rotations.TryGetValue(photo.Id, out var degrees)) photo.SetRotation(degrees);
    }

    private void OnLibraryReloaded()
    {
        Albums.Reload();
        Tags.Reload();
        RefreshTagBadges();
        if (Context is BrowseContext.Album or BrowseContext.Tag) RefreshContext();
    }

    private void OnAlbumChanged(long albumId)
    {
        RefreshTagBadges();   // numele albumelor fac parte din căutare
        if (Context == BrowseContext.Album) RefreshContext();
    }

    private void OnTagChanged(long tagId)
    {
        Details.RefreshTags();
        RefreshTagBadges();
        if (Context == BrowseContext.Tag) RefreshContext();
    }

    private void OnTagsAssignmentChanged()
    {
        Tags.Reload();
        RefreshTagBadges();
        if (Context == BrowseContext.Tag) RefreshContext();
    }

    // ------------------------------------------------------------------ Acțiuni pe pozele selectate

    private IReadOnlyList<long> SelectedIds() =>
        Grid.SelectedPhotos.Count > 0 ? Grid.SelectedPhotos.Select(p => p.Id).ToList()
        : Grid.SelectedPhoto is { } single ? [single.Id] : [];

    /// <summary>„Adaugă la album": album existent sau null = album nou (§6.5).</summary>
    [RelayCommand]
    private void AddToAlbum(AlbumItemViewModel? album)
    {
        var ids = SelectedIds();
        if (ids.Count == 0) return;
        album ??= Albums.PromptCreate();
        if (album is null) return;

        var added = _albumService.AddPhotos(album.Id, ids);
        Albums.Reload();
        RefreshTagBadges();
        Status.ShowMessage(Loc.Format("Str.Status.AddedToAlbum", Loc.PhotoCount(added), album.Name));
    }

    /// <summary>„Elimină din album" — doar din contextul unui album deschis; pozele rămân indexate.</summary>
    [RelayCommand]
    private void RemoveFromAlbum()
    {
        if (CurrentAlbum is not { } album) return;
        var ids = SelectedIds();
        if (ids.Count == 0) return;

        var removed = _albumService.RemovePhotos(album.Id, ids);
        Albums.Reload();
        RefreshTagBadges();
        RefreshContext();
        Status.ShowMessage(Loc.Format("Str.Status.RemovedFromAlbum", Loc.PhotoCount(removed), album.Name));
    }

    [RelayCommand]
    private void SetAlbumCover()
    {
        if (CurrentAlbum is not { } album || Grid.SelectedPhoto is not { } photo) return;
        _albumService.SetCover(album.Id, photo.Id);
        Albums.Reload();
        Status.ShowMessage(Loc.Format("Str.Status.CoverSet", album.Name));
    }

    /// <summary>„Adaugă tag": tag existent sau null = tag nou, pe toate pozele selectate (§6.6).</summary>
    [RelayCommand]
    private void AddTag(TagItemViewModel? tag)
    {
        var ids = SelectedIds();
        if (ids.Count == 0) return;
        tag ??= Tags.PromptCreate();
        if (tag is null) return;

        var added = _tagService.Assign(tag.Id, ids);
        Tags.Reload();
        Details.RefreshTags();
        RefreshTagBadges();
        if (Context == BrowseContext.Tag) RefreshContext();
        Status.ShowMessage(Loc.Format("Str.Status.TagAdded", tag.Name, Loc.PhotoCount(added)));
    }

    /// <summary>Din panoul de detalii: adaugă un tag doar pozei afișate.</summary>
    [RelayCommand]
    private void AddTagToCurrentPhoto(TagItemViewModel? tag)
    {
        if (Details.Photo is not { } photo) return;
        tag ??= Tags.PromptCreate();
        if (tag is null) return;

        _tagService.Assign(tag.Id, [photo.Id]);
        Tags.Reload();
        Details.RefreshTags();
        RefreshTagBadges();
        if (Context == BrowseContext.Tag) RefreshContext();
    }

    // ------------------------------------------------------------------ Bara secundară

    [RelayCommand]
    private void ToggleTheme() => SetTheme(IsDarkTheme ? AppTheme.Light : AppTheme.Dark);

    [RelayCommand]
    private void SetTheme(AppTheme theme)
    {
        if ((theme == AppTheme.Dark) == IsDarkTheme) return;
        _theme.SetTheme(theme);
        _settings.SetTheme(theme);
        IsDarkTheme = theme == AppTheme.Dark;
    }

    [RelayCommand]
    private async Task OpenBatchRename()
    {
        // Pozele indexate redenumite și-au păstrat Id-ul; se reîncarcă doar numele / căile afișate
        if (_windows.ShowBatchRename()) await Library.RefreshAsync();
    }

    /// <summary>Slideshow cu pozele afișate în grid (context + căutare + sortare), de la poza selectată (§6.10).</summary>
    [RelayCommand]
    private void StartSlideshow(PhotoItemViewModel? from)
    {
        if (!IsPhotosView || Grid.Photos.Count == 0) return;
        from ??= Grid.SelectedPhoto;
        var start = from is null ? 0 : Math.Max(0, Grid.Photos.IndexOf(from));
        var reached = _windows.ShowSlideshow(Grid.Photos.ToList(), start, ContextTitle);
        if (reached is not null) Grid.SelectedPhoto = reached;
    }

    [RelayCommand]
    private void OpenInfo() => _windows.ShowInfo();

    [RelayCommand]
    private void OpenSettings() => _windows.ShowSettings(this);

    [RelayCommand]
    private void OpenLogo() => _windows.ShowLogo();

    private async void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PhotoGridViewModel.SelectedPhoto)) return;
        await Details.ShowAsync(Grid.SelectedPhoto);
        OnPropertyChanged(nameof(DetailsMode));
    }
}
