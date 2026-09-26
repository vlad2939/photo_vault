namespace PhotoVault.Core.Models;

/// <summary>Album manual (tabela Albums). Structură pur logică.</summary>
public sealed class Album
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public long? CoverPhotoId { get; set; }
}
