using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;

namespace PhotoVault.App.ViewModels;

/// <summary>Folder sursă = rădăcină în arborele Bibliotecă; re-scanare / eliminare din meniul contextual.</summary>
public partial class SourceFolderViewModel : FolderNodeViewModel
{
    private readonly FolderTreeViewModel _owner;

    public SourceFolderViewModel(SourceFolder folder, int photoCount, FolderTreeViewModel owner)
        : base(folder.FolderPath)
    {
        _owner = owner;
        Id = folder.Id;
        PhotoCount = photoCount;
    }

    public long Id { get; }
    public string FolderPath => FullPath;
    public string DisplayName => Name;
    public override bool IsRoot => true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhotoCountText))]
    public partial int PhotoCount { get; set; }

    public string PhotoCountText => Loc.Number(PhotoCount);

    [RelayCommand]
    private Task Rescan() => _owner.RescanAsync(this);

    [RelayCommand]
    private Task Remove() => _owner.RemoveAsync(this);
}
