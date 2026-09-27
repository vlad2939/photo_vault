using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;
using PhotoVault.Core.Utils;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Utilitarul de redenumire batch (§6.8): folder de pe disc → pattern → previzualizare live → aplicare.
/// Nicio redenumire nu are loc fără ca numele noi să fie vizibile în listă și confirmate.
/// </summary>
public partial class BatchRenameViewModel : ObservableObject
{
    public const string DefaultPattern = "Foto_{counter:000}";

    private readonly IBatchRenameService _service;
    private readonly IDialogService _dialogs;
    private readonly IFolderPicker _folderPicker;
    private IReadOnlyList<RenameSource> _files = [];
    private IReadOnlyList<RenamePlanItem> _plan = [];

    public BatchRenameViewModel(IBatchRenameService service, IDialogService dialogs, IFolderPicker folderPicker)
    {
        _service = service;
        _dialogs = dialogs;
        _folderPicker = folderPicker;
        UpdatePreview();
    }

    /// <summary>true dacă cel puțin o poză din bibliotecă a fost actualizată (fereastra principală se reîncarcă).</summary>
    public bool LibraryChanged { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFolder), nameof(FolderName))]
    public partial string? FolderPath { get; set; }

    public bool HasFolder => FolderPath is not null;
    public string FolderName => FolderPath is null ? string.Empty : Path.GetFileName(Path.TrimEndingDirectorySeparator(FolderPath)) is { Length: > 0 } name ? name : FolderPath;

    [ObservableProperty]
    public partial string FileCountText { get; set; } = string.Empty;

    /// <summary>Mesajul informativ când folderul face parte din bibliotecă (gol altfel).</summary>
    [ObservableProperty]
    public partial string IndexedInfo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Pattern { get; set; } = DefaultPattern;

    [ObservableProperty]
    public partial string CounterStart { get; set; } = "1";

    /// <summary>0 = după nume, 1 = după data fișierului.</summary>
    [ObservableProperty]
    public partial int OrderIndex { get; set; }

    [ObservableProperty]
    public partial string PatternError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<RenameRowViewModel> Rows { get; set; } = [];

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasConflicts { get; set; }

    /// <summary>Mesajul din zona previzualizării când lista e goală.</summary>
    [ObservableProperty]
    public partial string EmptyMessage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool CanApply { get; set; }

    partial void OnPatternChanged(string value) => UpdatePreview();
    partial void OnCounterStartChanged(string value) => UpdatePreview();
    partial void OnOrderIndexChanged(int value) => UpdatePreview();

    [RelayCommand]
    private void ChooseFolder()
    {
        var folder = _folderPicker.PickFolder(Loc.Get("Str.Rename.PickFolder"));
        if (folder is null) return;
        FolderPath = folder;
        LoadFiles();
    }

    private void LoadFiles()
    {
        if (FolderPath is null) return;
        try
        {
            _files = _service.GetFiles(FolderPath);
            var indexed = _service.CountIndexed(FolderPath);
            IndexedInfo = indexed == 0 ? string.Empty : Loc.Format("Str.Rename.IndexedInfo", Loc.PhotoCount(indexed));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorLog.Write(ex, "BatchRename");
            _files = [];
            IndexedInfo = string.Empty;
            _dialogs.Show(Loc.Get("Str.Rename.Title"), Loc.Format("Str.Rename.ReadError", ex.Message), DialogKind.Error);
        }
        FileCountText = Loc.PhotoCount(_files.Count);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var pattern = RenamePattern.TryParse(Pattern, out var error, out var detail);
        var start = ParseCounterStart();
        PatternError = error switch
        {
            RenamePatternError.Empty => Loc.Get("Str.Rename.Error.Empty"),
            RenamePatternError.UnbalancedBrace => Loc.Get("Str.Rename.Error.Brace"),
            RenamePatternError.UnknownToken => Loc.Format("Str.Rename.Error.Token", detail),
            _ when start is null => Loc.Get("Str.Rename.Error.Start"),
            _ => string.Empty,
        };

        _plan = FolderPath is not null && pattern is not null && start is not null
            ? _service.BuildPlan(FolderPath, _files, pattern, start.Value, OrderIndex == 1 ? RenameOrder.ByDate : RenameOrder.ByName)
            : [];

        // Previzualizarea arată numele actuale chiar dacă pattern-ul e momentan invalid
        Rows = _plan.Count > 0
            ? _plan.Select(p => new RenameRowViewModel(p)).ToList()
            : _files.Select(f => new RenameRowViewModel(new RenamePlanItem(f, string.Empty, RenameStatus.Unchanged))).ToList();

        var toRename = _plan.Count(p => p.Status == RenameStatus.Ok);
        var conflicts = _plan.Count(p => p.IsConflict);
        var unchanged = _plan.Count(p => p.Status == RenameStatus.Unchanged);
        HasConflicts = conflicts > 0;
        CanApply = toRename > 0 && conflicts == 0 && PatternError.Length == 0;

        var parts = new List<string>();
        if (_plan.Count > 0)
        {
            parts.Add(Loc.Format("Str.Rename.Summary.ToRename", Loc.FileCount(toRename)));
            if (conflicts > 0) parts.Add(Loc.Format("Str.Rename.Summary.Conflicts", Loc.FileCount(conflicts)));
            if (unchanged > 0) parts.Add(Loc.Format("Str.Rename.Summary.Unchanged", Loc.FileCount(unchanged)));
        }
        Summary = string.Join("  ·  ", parts);

        EmptyMessage = FolderPath is null ? Loc.Get("Str.Rename.NoFolder")
            : _files.Count == 0 ? Loc.Get("Str.Rename.NoFiles")
            : string.Empty;
    }

    private int? ParseCounterStart() =>
        int.TryParse(CounterStart.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value)
        && value <= 999_999_999 ? value : null;

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        if (FolderPath is null || !CanApply) return;
        var count = _plan.Count(p => p.Status == RenameStatus.Ok);
        var message = Loc.Format("Str.Rename.ConfirmMessage", Loc.FileCount(count), FolderName);
        if (IndexedInfo.Length > 0) message += "\n\n" + Loc.Get("Str.Rename.ConfirmIndexed");

        var answer = _dialogs.Show(Loc.Get("Str.Rename.ConfirmTitle"), message, DialogKind.Warning, DialogButtons.YesNo,
            primaryText: Loc.Get("Str.Rename.ConfirmYes"), secondaryText: Loc.Get("Str.Dialog.Cancel"));
        if (answer != DialogResultKind.Yes) return;

        try
        {
            var result = _service.Apply(FolderPath, _plan);
            if (result.IndexUpdated > 0) LibraryChanged = true;
            var done = Loc.Format("Str.Rename.Done", Loc.Number(result.Renamed));
            if (result.IndexUpdated > 0) done += "\n\n" + Loc.Format("Str.Rename.DoneIndexed", Loc.PhotoCount(result.IndexUpdated));
            _dialogs.Show(Loc.Get("Str.Rename.Title"), done, DialogKind.Success);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            ErrorLog.Write(ex, "BatchRename");
            _dialogs.Show(Loc.Get("Str.Rename.Title"), Loc.Format("Str.Rename.Failed", ex.Message), DialogKind.Error);
        }
        LoadFiles();   // numele de pe disc s-au schimbat → previzualizarea se reface
    }
}

/// <summary>Un rând din previzualizare.</summary>
public sealed class RenameRowViewModel(RenamePlanItem item)
{
    public string OriginalName => item.OriginalName;
    public string NewName => item.NewName;
    public RenameStatus Status => item.Status;
    public bool IsConflict => item.IsConflict;

    public string StatusText => NewName.Length == 0 ? string.Empty : Loc.Get(Status switch
    {
        RenameStatus.Ok => "Str.Rename.Status.Ok",
        RenameStatus.Unchanged => "Str.Rename.Status.Unchanged",
        RenameStatus.Invalid => "Str.Rename.Status.Invalid",
        RenameStatus.Duplicate => "Str.Rename.Status.Duplicate",
        _ => "Str.Rename.Status.Exists",
    });

    public override string ToString() => NewName.Length == 0 ? OriginalName : $"{OriginalName} → {NewName}";
}
