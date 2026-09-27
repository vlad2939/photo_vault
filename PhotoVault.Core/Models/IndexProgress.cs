namespace PhotoVault.Core.Models;

/// <summary>Etapa curentă a unei operațiuni de indexare.</summary>
public enum IndexPhase
{
    /// <summary>Enumerarea fișierelor de pe disc (total încă necunoscut).</summary>
    Scanning,
    /// <summary>Inserarea în index a pozelor găsite.</summary>
    Indexing,
    /// <summary>Generarea miniaturilor.</summary>
    Thumbnails
}

/// <summary>Raport de progres trimis către UI (footer, §5.7).</summary>
public readonly record struct IndexProgress(IndexPhase Phase, int Current, int Total);

/// <summary>Rezultatul unei (re)scanări de folder sursă.</summary>
public sealed record ScanResult(int Added, int Removed, int Total);

/// <summary>Rezultatul generării unei miniaturi.</summary>
/// <param name="ThumbnailPath">Cale relativă în data/thumbnails/, sau null dacă imaginea nu a putut fi citită.</param>
public sealed record ThumbnailResult(long PhotoId, string? ThumbnailPath, DateTime? DateTaken);
