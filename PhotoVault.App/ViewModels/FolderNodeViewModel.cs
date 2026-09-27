using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Nod din arborele de foldere (§5.7). Subfolderele se citesc de pe disc doar la
/// prima expandare (lazy-load) — pornirea rămâne rapidă indiferent de adâncimea arborelui.
/// </summary>
public partial class FolderNodeViewModel : ObservableObject
{
    private static readonly EnumerationOptions DirectoryOptions = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
    };

    /// <summary>Copil fictiv: face vizibilă săgeata de expandare până la încărcarea reală.</summary>
    private static readonly FolderNodeViewModel Placeholder = new();

    private bool _childrenLoaded;

    private FolderNodeViewModel()
    {
        Name = FullPath = string.Empty;
        _childrenLoaded = true;
    }

    public FolderNodeViewModel(string fullPath, string? displayName = null)
    {
        FullPath = fullPath;
        var name = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        Name = displayName ?? (string.IsNullOrEmpty(name) ? fullPath : name);   // ex. rădăcina unui disc „D:\"
        if (HasSubdirectories(fullPath)) Children.Add(Placeholder);
        else _childrenLoaded = true;
    }

    public string Name { get; }
    public string FullPath { get; }
    public ObservableCollection<FolderNodeViewModel> Children { get; } = [];

    /// <summary>Folder sursă (rădăcină) — are meniu contextual și contor de poze.</summary>
    public virtual bool IsRoot => false;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !_childrenLoaded) LoadChildren();
    }

    private void LoadChildren()
    {
        _childrenLoaded = true;
        Children.Clear();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(FullPath, "*", DirectoryOptions)
                         .OrderBy(d => d, StringComparer.CurrentCultureIgnoreCase))
                Children.Add(new FolderNodeViewModel(dir));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Folder devenit inaccesibil — nodul rămâne fără copii.
        }
    }

    private static bool HasSubdirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path, "*", DirectoryOptions).Any();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
