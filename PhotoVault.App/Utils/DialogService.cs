using System.Windows;
using PhotoVault.App.Views;

namespace PhotoVault.App.Utils;

/// <summary>Afișează <see cref="CustomDialogWindow"/> ca overlay modal peste fereastra activă.</summary>
public sealed class DialogService : IDialogService
{
    public DialogResultKind Show(string title, string message,
        DialogKind kind = DialogKind.Info,
        DialogButtons buttons = DialogButtons.Ok,
        Window? owner = null,
        string? primaryText = null,
        string? secondaryText = null)
    {
        var dialog = new CustomDialogWindow(title, message, kind, buttons);
        dialog.SetButtonTexts(primaryText, secondaryText);
        dialog.AttachTo(ResolveOwner(owner));
        dialog.ShowDialog();
        return dialog.Result;
    }

    public string? Prompt(string title, string message, string initialText = "", string placeholder = "", Window? owner = null)
    {
        var dialog = new CustomDialogWindow(title, message, DialogKind.Info, DialogButtons.OkCancel);
        dialog.EnableInput(initialText, placeholder);
        dialog.AttachTo(ResolveOwner(owner));
        dialog.ShowDialog();
        return dialog.Result == DialogResultKind.Ok && dialog.InputText.Length > 0 ? dialog.InputText : null;
    }

    public (string Name, string? Subtitle)? PromptAlbum(string title, string message, string name = "", string? subtitle = null)
    {
        var dialog = new CustomDialogWindow(title, message, DialogKind.Info, DialogButtons.OkCancel);
        dialog.EnableInput(name, Loc.Get("Str.Albums.NamePlaceholder"), Loc.Get("Str.Albums.NameLabel"));
        dialog.EnableSecondaryInput(Loc.Get("Str.Albums.SubtitleLabel"), subtitle ?? string.Empty, Loc.Get("Str.Albums.SubtitlePlaceholder"));
        dialog.AttachTo(ResolveOwner(null));
        dialog.ShowDialog();
        if (dialog.Result != DialogResultKind.Ok || dialog.InputText.Length == 0) return null;
        return (dialog.InputText, dialog.SecondaryText.Length == 0 ? null : dialog.SecondaryText);
    }

    private static Window? ResolveOwner(Window? owner)
    {
        owner ??= Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                  ?? Application.Current?.MainWindow;
        if (owner is CustomDialogWindow nested) owner = nested.Owner;   // dialog peste dialog → același părinte
        return owner is { IsVisible: false } ? null : owner;
    }
}
