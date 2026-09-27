using Microsoft.Win32;

namespace PhotoVault.App.Utils;

/// <summary>Alegerea unui folder de pe disc (abstracție pentru ViewModel-uri).</summary>
public interface IFolderPicker
{
    string? PickFolder(string title);
}

/// <summary>Dialogul nativ Windows de selecție folder (OpenFolderDialog, .NET 8+).</summary>
public sealed class FolderPicker : IFolderPicker
{
    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title, Multiselect = false };
        var app = System.Windows.Application.Current;
        var owner = app.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive) ?? app.MainWindow;
        return dialog.ShowDialog(owner) == true ? dialog.FolderName : null;
    }
}
