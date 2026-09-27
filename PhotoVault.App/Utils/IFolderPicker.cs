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

/// <summary>Alegerea unuia sau mai multor fișiere (ex. piese MP3 pentru slideshow).</summary>
public interface IFilePicker
{
    IReadOnlyList<string> PickFiles(string title, string filter);
}

/// <summary>Dialogul nativ Windows de deschidere fișiere, cu selecție multiplă.</summary>
public sealed class FilePicker : IFilePicker
{
    public IReadOnlyList<string> PickFiles(string title, string filter)
    {
        var dialog = new OpenFileDialog { Title = title, Filter = filter, Multiselect = true, CheckFileExists = true };
        var app = System.Windows.Application.Current;
        var owner = app.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive) ?? app.MainWindow;
        return dialog.ShowDialog(owner) == true ? dialog.FileNames : [];
    }
}
