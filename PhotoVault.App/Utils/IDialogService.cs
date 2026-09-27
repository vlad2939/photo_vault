using System.Windows;

namespace PhotoVault.App.Utils;

public enum DialogKind
{
    Info,
    Success,
    Warning,
    Error
}

public enum DialogButtons
{
    Ok,
    OkCancel,
    YesNo
}

public enum DialogResultKind
{
    None,
    Ok,
    Cancel,
    Yes,
    No
}

/// <summary>
/// Mesaje de informare/avertizare/eroare stilizate cu tema aplicației —
/// înlocuiește complet MessageBox.Show() (§5.1).
/// </summary>
public interface IDialogService
{
    DialogResultKind Show(string title, string message,
        DialogKind kind = DialogKind.Info,
        DialogButtons buttons = DialogButtons.Ok,
        Window? owner = null);

    /// <summary>Cere un text (ex. numele unui album); null dacă utilizatorul a anulat.</summary>
    string? Prompt(string title, string message, string initialText = "", string placeholder = "", Window? owner = null);
}
