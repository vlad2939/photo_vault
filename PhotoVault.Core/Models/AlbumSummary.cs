namespace PhotoVault.Core.Models;

/// <summary>Album + informațiile afișate pe card (§5.7): număr de poze și miniatura copertei.</summary>
/// <param name="CoverThumbnailPath">Calea relativă a miniaturii copertei (setată manual sau prima poză adăugată).</param>
public sealed record AlbumSummary(Album Album, int PhotoCount, long? CoverPhotoId, string? CoverThumbnailPath);

/// <summary>Tag + numărul de poze care îl au.</summary>
public sealed record TagSummary(Tag Tag, int PhotoCount);
