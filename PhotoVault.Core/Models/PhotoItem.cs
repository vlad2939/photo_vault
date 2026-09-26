namespace PhotoVault.Core.Models;

/// <summary>Fotografie indexată (tabela Photos). Fișierul original nu este niciodată modificat.</summary>
public sealed class PhotoItem
{
    public long Id { get; set; }
    public long SourceFolderId { get; set; }
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    /// <summary>Extensia fără punct, lowercase (jpg, png, cr2, nef, dng).</summary>
    public string Extension { get; set; } = string.Empty;

    public long? FileSizeBytes { get; set; }
    public DateTime DateAdded { get; set; }
    public DateTime? DateTakenExif { get; set; }

    /// <summary>Rotire logică: 0, 90, 180 sau 270 de grade.</summary>
    public int RotationDegrees { get; set; }

    /// <summary>Cale relativă în data/thumbnails/.</summary>
    public string? ThumbnailPath { get; set; }

    public bool IsMissing { get; set; }
}
