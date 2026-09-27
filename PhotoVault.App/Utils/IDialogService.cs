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
    /// <param name="primaryText">Etichetă proprie pentru butonul principal (implicit OK / Da).</param>
    /// <param name="secondaryText">Etichetă proprie pentru butonul secundar (implicit Anulează / Nu).</param>
    DialogResultKind Show(string title, string message,
        DialogKind kind = DialogKind.Info,
        DialogButtons buttons = DialogButtons.Ok,
        Window? owner = null,
        string? primaryText = null,
        string? secondaryText = null);

    /// <summary>Cere un text (ex. numele unui album); null dacă utilizatorul a anulat.</summary>
    string? Prompt(string title, string message, string initialText = "", string placeholder = "", Window? owner = null);

    /// <summary>Nume (obligatoriu) + subtitlu (opțional) pentru un album; null dacă s-a anulat.</summary>
    (string Name, string? Subtitle)? PromptAlbum(string title, string message, string name = "", string? subtitle = null);
}
