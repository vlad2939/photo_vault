using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Secțiunea „Bibliotecă" din panoul stâng: foldere sursă + adăugare / re-scanare / eliminare (§6.2).
/// Indexarea rulează în fundal; UI-ul rămâne utilizabil, progresul apare în footer.
/// </summary>
public partial class FolderTreeViewModel : ObservableObject
{
    private readonly IPhotoIndexService _index;
    private readonly IDialogService _dialogs;
    private readonly IFolderPicker _folderPicker;
    private readonly StatusBarViewModel _status;
    private readonly PhotoGridViewModel _grid;
    private readonly CancellationToken _shutdown;

    public FolderTreeViewModel(IPhotoIndexService index, IDialogService dialogs, IFolderPicker folderPicker,
        StatusBarViewModel status, PhotoGridViewModel grid, CancellationToken shutdown)
    {
        _index = index;
        _dialogs = dialogs;
        _folderPicker = folderPicker;
        _status = status;
        _grid = grid;
        _shutdown = shutdown;
    }

    public ObservableCollection<SourceFolderViewModel> Folders { get; } = [];

    [ObservableProperty]
    public partial bool HasFolders { get; set; }

    /// <summary>Încărcarea inițială, la pornire: foldere + poze din index, apoi miniaturile rămase negenerate.</summary>
    public async Task InitializeAsync()
    {
        await ReloadAsync();
        StartThumbnails();
    }

    [RelayCommand]
    private async Task AddFolder()
    {
        var path = _folderPicker.PickFolder(Loc.Get("Str.Library.PickFolder"));
        if (path is null) return;

        var (validation, conflict) = _index.ValidateNewFolder(path);
        var messageKey = validation switch
        {
            FolderValidation.NotFound => "Str.Library.NotFound",
            FolderValidation.AlreadyAdded => "Str.Library.AlreadyAdded",
            FolderValidation.InsideExisting => "Str.Library.InsideExisting",
            FolderValidation.ContainsExisting => "Str.Library.ContainsExisting",
            _ => null,
        };
        if (messageKey is not null)
        {
            _dialogs.Show(Loc.Get("Str.Library.CannotAddTitle"), Loc.Format(messageKey, path, conflict), DialogKind.Warning);
            return;
        }

        await RunScanAsync(async progress =>
        {
            var (_, result) = await _index.AddSourceFolderAsync(path, progress, _shutdown);
            return Loc.Format("Str.Status.IndexDone", Loc.Number(result.Added));
        });
    }

    public async Task RescanAsync(SourceFolderViewModel folder)
    {
        await RunScanAsync(async progress =>
        {
            try
            {
                var result = await _index.RescanAsync(folder.Id, progress, _shutdown);
                return Loc.Format("Str.Status.RescanDone", Loc.Number(result.Added), Loc.Number(result.Removed));
            }
            catch (DirectoryNotFoundException)
            {
                _dialogs.Show(Loc.Get("Str.Library.UnavailableTitle"),
                    Loc.Format("Str.Library.Unavailable", folder.FolderPath), DialogKind.Warning);
                return null;
            }
        });
    }

    public async Task RemoveAsync(SourceFolderViewModel folder)
    {
        var answer = _dialogs.Show(Loc.Get("Str.Library.RemoveTitle"),
            Loc.Format("Str.Library.RemoveConfirm", folder.FolderPath, Loc.Number(folder.PhotoCount)),
            DialogKind.Warning, DialogButtons.YesNo);
        if (answer != DialogResultKind.Yes) return;

        await _index.RemoveSourceFolderAsync(folder.Id);
        await ReloadAsync();
        _status.ShowMessage(Loc.Format("Str.Status.Removed", folder.DisplayName));
    }

    /// <summary>Rulează o scanare cu progres în footer, apoi reîncarcă grid-ul și pornește miniaturile.</summary>
    private async Task RunScanAsync(Func<IProgress<IndexProgress>, Task<string?>> scan)
    {
        var progress = new Progress<IndexProgress>(_status.Report);   // creat pe thread-ul UI → raportări marshal-ate automat
        _status.BeginScan();
        string? message;
        try
        {
            message = await scan(progress);
        }
        catch (OperationCanceledException)
        {
            return;   // aplicația se închide
        }
        finally
        {
            _status.EndScan();
        }

        await ReloadAsync();
        if (message is not null) _status.ShowMessage(message);
        StartThumbnails();
    }

    private async Task ReloadAsync()
    {
        var (folders, counts, photos) = await Task.Run(() =>
            (_index.GetSourceFolders(), _index.GetPhotoCounts(), _index.GetAllPhotos()));

        Folders.Clear();
        foreach (var folder in folders)
            Folders.Add(new SourceFolderViewModel(folder, counts.GetValueOrDefault(folder.Id), this));
        HasFolders = Folders.Count > 0;

        _grid.Load(photos);
    }

    private async void StartThumbnails()
    {
        try
        {
            await _index.EnsureThumbnailsAsync(
                new Progress<IndexProgress>(_status.Report),
                new Progress<ThumbnailResult>(_grid.ApplyThumbnail),
                _shutdown);
        }
        catch (OperationCanceledException)
        {
            // închiderea aplicației
        }
    }
}
