namespace PhotoVault.Core.Models;

/// <summary>Folder sursă adăugat manual de utilizator (tabela SourceFolders).</summary>
public sealed class SourceFolder
{
    public long Id { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; }
    public DateTime? LastScanned { get; set; }
}
