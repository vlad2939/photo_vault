using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;

namespace PhotoVault.App.ViewModels;

/// <summary>Un album: rând în lista din stânga și card în vizualizarea „Albume" (§5.7).</summary>
public partial class AlbumItemViewModel : ObservableObject
{
    private readonly AlbumViewModel _owner;
    private bool _coverRequested;

    public AlbumItemViewModel(AlbumSummary summary, string? coverAbsolutePath, AlbumViewModel owner)
    {
        _owner = owner;
        Id = summary.Album.Id;
        Name = summary.Album.Name;
        DateCreated = summary.Album.DateCreated;
        PhotoCount = summary.PhotoCount;
        CoverPhotoId = summary.CoverPhotoId;
        CoverAbsolutePath = coverAbsolutePath;
    }

    public long Id { get; }
    public string Name { get; }
    public DateTime DateCreated { get; }
    public int PhotoCount { get; }
    public long? CoverPhotoId { get; }
    public string? CoverAbsolutePath { get; }

    public string DateText => DateCreated.ToString("dd.MM.yyyy", Loc.Culture);
    public string PhotoCountText => Loc.Number(PhotoCount);
    public string PhotoCountLabel => Loc.PhotoCount(PhotoCount);

    /// <summary>Miniatura copertei, încărcată leneș (cardurile vizibile).</summary>
    public ImageSource? Cover
    {
        get
        {
            if (!_coverRequested && !string.IsNullOrEmpty(CoverAbsolutePath))
            {
                _coverRequested = true;
                _ = LoadCoverAsync(CoverAbsolutePath);
            }
            return field;
        }
        private set => SetProperty(ref field, value);
    }

    private async Task LoadCoverAsync(string path) => Cover = await ThumbnailCache.GetAsync(path);

    /// <summary>Numele (folosit și de UI Automation / cititoare de ecran).</summary>
    public override string ToString() => Name;

    [RelayCommand]
    private void Open() => _owner.RequestOpen(this);

    [RelayCommand]
    private void Rename() => _owner.RenameAlbum(this);

    [RelayCommand]
    private void Delete() => _owner.DeleteAlbum(this);
}
