using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;

namespace PhotoVault.App.ViewModels;

/// <summary>Folder sursă = rădăcină în arborele Bibliotecă; re-scanare / eliminare din meniul contextual.</summary>
public partial class SourceFolderViewModel : FolderNodeViewModel
{
    private readonly FolderTreeViewModel _owner;

    public SourceFolderViewModel(SourceFolder folder, int photoCount, bool isAvailable, FolderTreeViewModel owner)
        : base(folder.FolderPath)
    {
        _owner = owner;
        Id = folder.Id;
        PhotoCount = photoCount;
        IsAvailable = isAvailable;
    }

    /// <summary>
    /// false dacă folderul nu e accesibil acum (disc extern deconectat, altă literă de disc, alt calculator):
    /// pozele rămân în bibliotecă, iar folderul poate fi realiniat cu „Schimbă locația...".
    /// </summary>
    public override bool IsAvailable { get; }

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

    [RelayCommand]
    private Task Relocate() => _owner.RelocateAsync(this);
}
