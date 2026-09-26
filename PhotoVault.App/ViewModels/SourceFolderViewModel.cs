using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;

namespace PhotoVault.App.ViewModels;

/// <summary>Un folder sursă din secțiunea Bibliotecă (Faza 2 îl extinde cu arborele de subfoldere).</summary>
public partial class SourceFolderViewModel : ObservableObject
{
    private readonly FolderTreeViewModel _owner;

    public SourceFolderViewModel(SourceFolder folder, int photoCount, FolderTreeViewModel owner)
    {
        _owner = owner;
        Id = folder.Id;
        FolderPath = folder.FolderPath;
        PhotoCount = photoCount;
        var name = Path.GetFileName(folder.FolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        DisplayName = string.IsNullOrEmpty(name) ? folder.FolderPath : name;   // ex. rădăcina unui disc „D:\"
    }

    public long Id { get; }
    public string FolderPath { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhotoCountText))]
    public partial int PhotoCount { get; set; }

    public string PhotoCountText => Loc.Number(PhotoCount);

    [RelayCommand]
    private Task Rescan() => _owner.RescanAsync(this);

    [RelayCommand]
    private Task Remove() => _owner.RemoveAsync(this);
}
