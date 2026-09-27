using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Abstractions;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Secțiunea „Copii de siguranță" din Opțiuni (§12.1): starea copiilor automate, copie manuală la cerere și
/// deschiderea folderului data\ (restaurarea se face manual, cu aplicația închisă — vezi README).
/// </summary>
public partial class BackupSettingsViewModel : ObservableObject
{
    private readonly IDatabaseBackup _backup;
    private readonly IDialogService _dialogs;

    public BackupSettingsViewModel(IDatabaseBackup backup, IDialogService dialogs)
    {
        _backup = backup;
        _dialogs = dialogs;
        Refresh();
    }

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    public string HintText => Loc.Format("Str.Backup.Hint", _backup.MaxBackups);

    /// <summary>Reîmprospătat la fiecare deschidere a ferestrei Opțiuni.</summary>
    public void Refresh()
    {
        var backups = _backup.GetBackups();
        StatusText = backups.Count == 0
            ? Loc.Format("Str.Backup.None", _backup.MaxBackups)
            : Loc.Format("Str.Backup.Status", backups[0].Created.ToString("g", Loc.Culture), Loc.Number(backups.Count), _backup.MaxBackups);
    }

    [RelayCommand]
    private void CreateNow()
    {
        var info = _backup.CreateBackup(force: true);
        Refresh();
        if (info is null)
            _dialogs.Show(Loc.Get("Str.Backup.Title"), Loc.Get("Str.Backup.Failed"), DialogKind.Warning);
        else
            _dialogs.Show(Loc.Get("Str.Backup.Title"), Loc.Format("Str.Backup.Created", Path.GetFileName(info.Path)), DialogKind.Success);
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var folder = _backup.GetBackups().FirstOrDefault() is { } latest
            ? Path.GetDirectoryName(latest.Path)
            : Path.Combine(AppContext.BaseDirectory, "data");
        if (folder is null || !Directory.Exists(folder)) return;
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }
}
