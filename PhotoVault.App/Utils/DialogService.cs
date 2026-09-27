using System.Windows;
using PhotoVault.App.Views;

namespace PhotoVault.App.Utils;

/// <summary>Afișează <see cref="CustomDialogWindow"/> ca overlay modal peste fereastra activă.</summary>
public sealed class DialogService : IDialogService
{
    public DialogResultKind Show(string title, string message,
        DialogKind kind = DialogKind.Info,
        DialogButtons buttons = DialogButtons.Ok,
        Window? owner = null)
    {
        var dialog = new CustomDialogWindow(title, message, kind, buttons);
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

    private static Window? ResolveOwner(Window? owner)
    {
        owner ??= Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                  ?? Application.Current?.MainWindow;
        if (owner is CustomDialogWindow nested) owner = nested.Owner;   // dialog peste dialog → același părinte
        return owner is { IsVisible: false } ? null : owner;
    }
}
