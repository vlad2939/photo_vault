namespace PhotoVault.Core.Models;

/// <summary>Album manual (tabela Albums). Structură pur logică.</summary>
public sealed class Album
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Subtitlu liber, scris de utilizator (ex. perioada: „10–15.08.2021").</summary>
    public string? Subtitle { get; set; }
    public DateTime DateCreated { get; set; }
    public long? CoverPhotoId { get; set; }
}
