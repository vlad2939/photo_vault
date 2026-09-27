using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>Tag-urile custom (§6.6): listă în panoul stâng, creare / redenumire / ștergere, filtrare.</summary>
public partial class TagViewModel(ITagService tags, IDialogService dialogs) : ObservableObject
{
    public ObservableCollection<TagItemViewModel> Tags { get; } = [];

    [ObservableProperty]
    public partial bool HasTags { get; set; }

    /// <summary>Tag-ul după care e filtrat grid-ul (evidențiat în listă).</summary>
    [ObservableProperty]
    public partial TagItemViewModel? ActiveTag { get; set; }

    /// <summary>Utilizatorul a selectat un tag în listă.</summary>
    public event Action<TagItemViewModel>? FilterRequested;

    /// <summary>Un tag a fost redenumit / șters (contextul și detaliile trebuie actualizate).</summary>
    public event Action<long>? TagChanged;

    partial void OnActiveTagChanged(TagItemViewModel? value)
    {
        if (value is not null && !_reloading) FilterRequested?.Invoke(value);
    }

    private bool _reloading;

    public void Reload()
    {
        var activeId = ActiveTag?.Id;
        _reloading = true;
        try
        {
            Tags.Clear();
            foreach (var summary in tags.GetTags()) Tags.Add(new TagItemViewModel(summary, this));
            HasTags = Tags.Count > 0;
            ActiveTag = Tags.FirstOrDefault(t => t.Id == activeId);
        }
        finally
        {
            _reloading = false;
        }
    }

    public TagItemViewModel? Find(long id) => Tags.FirstOrDefault(t => t.Id == id);

    /// <summary>Tag nou; dacă există deja unul cu același nume, e refolosit.</summary>
    public TagItemViewModel? PromptCreate()
    {
        var name = dialogs.Prompt(Loc.Get("Str.Tags.NewTitle"), Loc.Get("Str.Tags.NewMessage"),
            placeholder: Loc.Get("Str.Tags.NamePlaceholder"));
        if (name is null) return null;

        var tag = tags.GetOrCreate(name);
        Reload();
        return Find(tag.Id);
    }

    [RelayCommand]
    private void CreateTag() => PromptCreate();

    public void RenameTag(TagItemViewModel tag)
    {
        var name = dialogs.Prompt(Loc.Get("Str.Tags.RenameTitle"), Loc.Get("Str.Tags.RenameMessage"), tag.Name);
        if (name is null || name == tag.Name) return;
        if (!tags.TryRename(tag.Id, name))
        {
            dialogs.Show(Loc.Get("Str.Tags.RenameTitle"), Loc.Format("Str.Tags.Duplicate", name), DialogKind.Warning);
            return;
        }
        Reload();
        TagChanged?.Invoke(tag.Id);
    }

    public void DeleteTag(TagItemViewModel tag)
    {
        var answer = dialogs.Show(Loc.Get("Str.Tags.DeleteTitle"),
            Loc.Format("Str.Tags.DeleteConfirm", tag.Name, Loc.Number(tag.PhotoCount)),
            DialogKind.Warning, DialogButtons.YesNo);
        if (answer != DialogResultKind.Yes) return;
        tags.Delete(tag.Id);
        if (ActiveTag?.Id == tag.Id) ActiveTag = null;
        Reload();
        TagChanged?.Invoke(tag.Id);
    }
}
