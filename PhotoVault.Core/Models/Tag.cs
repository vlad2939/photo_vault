namespace PhotoVault.Core.Models;

/// <summary>Etichetă custom (tabela Tags).</summary>
public sealed class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
