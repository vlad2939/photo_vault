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
        owner ??= Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                  ?? Application.Current?.MainWindow;
        if (owner is CustomDialogWindow nested) owner = nested.Owner;   // dialog peste dialog → același părinte
        if (owner is { IsVisible: false }) owner = null;

        var dialog = new CustomDialogWindow(title, message, kind, buttons);
        dialog.AttachTo(owner);
        dialog.ShowDialog();
        return dialog.Result;
    }
}
