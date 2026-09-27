using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;

namespace PhotoVault.App.ViewModels;

/// <summary>Un tag: rând în lista din stânga (cu numărul de poze) și opțiune în meniurile „Adaugă tag".</summary>
public partial class TagItemViewModel(TagSummary summary, TagViewModel owner)
{
    public long Id { get; } = summary.Tag.Id;
    public string Name { get; } = summary.Tag.Name;
    public int PhotoCount { get; } = summary.PhotoCount;
    public string PhotoCountText => Loc.Number(PhotoCount);

    /// <summary>Numele (folosit și de UI Automation / cititoare de ecran).</summary>
    public override string ToString() => Name;

    [RelayCommand]
    private void Rename() => owner.RenameTag(this);

    [RelayCommand]
    private void Delete() => owner.DeleteTag(this);
}
