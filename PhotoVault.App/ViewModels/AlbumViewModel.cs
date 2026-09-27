using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Albumele (§6.5): lista din panoul stâng + grid-ul de carduri din zona centrală.
/// Creare / redenumire / ștergere — doar structura logică, fișierele de pe disc nu sunt atinse.
/// </summary>
public partial class AlbumViewModel(IAlbumService albums, IThumbnailService thumbnails, IDialogService dialogs) : ObservableObject
{
    public ObservableCollection<AlbumItemViewModel> Albums { get; } = [];

    [ObservableProperty]
    public partial bool HasAlbums { get; set; }

    /// <summary>Albumul deschis (evidențiat în lista din stânga); null când nu e deschis niciun album.</summary>
    [ObservableProperty]
    public partial AlbumItemViewModel? ActiveAlbum { get; set; }

    /// <summary>Cardul selectat în vizualizarea „Albume" — sursa panoului de detalii.</summary>
    [ObservableProperty]
    public partial AlbumItemViewModel? SelectedAlbum { get; set; }

    /// <summary>Deschiderea unui album (click în listă / dublu-click pe card).</summary>
    public event Action<AlbumItemViewModel>? OpenRequested;

    /// <summary>Un album a fost șters sau redenumit (contextul curent poate trebui actualizat).</summary>
    public event Action<long>? AlbumChanged;

    private bool _reloading;

    partial void OnActiveAlbumChanged(AlbumItemViewModel? value)
    {
        // Selecția în lista din stânga deschide albumul (nu și re-selecția automată de după Reload)
        if (value is not null && !_reloading) OpenRequested?.Invoke(value);
    }

    public void Reload()
    {
        var activeId = ActiveAlbum?.Id;
        var selectedId = SelectedAlbum?.Id;
        _reloading = true;
        try
        {
            ReloadCore(activeId, selectedId);
        }
        finally
        {
            _reloading = false;
        }
    }

    private void ReloadCore(long? activeId, long? selectedId)
    {
        Albums.Clear();
        foreach (var summary in albums.GetAlbums())
        {
            var cover = string.IsNullOrEmpty(summary.CoverThumbnailPath) ? null : thumbnails.GetAbsolutePath(summary.CoverThumbnailPath);
            Albums.Add(new AlbumItemViewModel(summary, cover, this));
        }
        HasAlbums = Albums.Count > 0;

        ActiveAlbum = Albums.FirstOrDefault(a => a.Id == activeId);
        SelectedAlbum = Albums.FirstOrDefault(a => a.Id == selectedId);
    }

    public AlbumItemViewModel? Find(long id) => Albums.FirstOrDefault(a => a.Id == id);

    public void RequestOpen(AlbumItemViewModel album) => OpenRequested?.Invoke(album);

    /// <summary>Album nou (buton + sau „Adaugă la album → Album nou…"); întoarce albumul creat.</summary>
    public AlbumItemViewModel? PromptCreate()
    {
        var input = PromptUnique(Loc.Get("Str.Albums.NewTitle"), Loc.Get("Str.Albums.NewMessage"), string.Empty, null, null);
        if (input is not { } value) return null;

        var album = albums.Create(value.Name, value.Subtitle);
        Reload();
        return Find(album.Id);
    }

    /// <summary>
    /// Formularul nume + subtitlu, repetat cât timp numele e deja folosit de alt album:
    /// utilizatorul poate modifica numele (formularul se redeschide cu ce a scris) sau renunța.
    /// </summary>
    private (string Name, string? Subtitle)? PromptUnique(string title, string message, string name, string? subtitle, long? exceptAlbumId)
    {
        while (true)
        {
            var input = dialogs.PromptAlbum(title, message, name, subtitle);
            if (input is not { } value) return null;
            if (!albums.IsNameTaken(value.Name, exceptAlbumId)) return value;

            var answer = dialogs.Show(Loc.Get("Str.Albums.DuplicateTitle"), Loc.Format("Str.Albums.Duplicate", value.Name),
                DialogKind.Warning, DialogButtons.OkCancel,
                primaryText: Loc.Get("Str.Albums.ChangeName"), secondaryText: Loc.Get("Str.Albums.GiveUp"));
            if (answer != DialogResultKind.Ok) return null;
            (name, subtitle) = value;
        }
    }

    [RelayCommand]
    private void CreateAlbum() => PromptCreate();

    /// <summary>Editează numele și subtitlul albumului.</summary>
    public void EditAlbum(AlbumItemViewModel album)
    {
        var input = PromptUnique(Loc.Get("Str.Albums.EditTitle"), Loc.Get("Str.Albums.EditMessage"), album.Name, album.Subtitle, album.Id);
        if (input is not { } value || (value.Name == album.Name && value.Subtitle == album.Subtitle)) return;
        albums.Update(album.Id, value.Name, value.Subtitle);
        Reload();
        AlbumChanged?.Invoke(album.Id);
    }

    public void DeleteAlbum(AlbumItemViewModel album)
    {
        var answer = dialogs.Show(Loc.Get("Str.Albums.DeleteTitle"),
            Loc.Format("Str.Albums.DeleteConfirm", album.Name, Loc.PhotoCount(album.PhotoCount)),
            DialogKind.Warning, DialogButtons.YesNo);
        if (answer != DialogResultKind.Yes) return;
        albums.Delete(album.Id);
        Reload();
        AlbumChanged?.Invoke(album.Id);
    }
}
