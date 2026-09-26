using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

/// <summary>Generare și gestiune a miniaturilor din data/thumbnails/ (§6.3).</summary>
public interface IThumbnailService
{
    /// <summary>Latura maximă a miniaturii, în pixeli (aspect ratio păstrat).</summary>
    int ThumbnailSize { get; }

    /// <summary>
    /// Generează miniatura (dacă nu există deja) și citește data EXIF.
    /// Nu aruncă excepții pentru fișiere ilizibile — întoarce ThumbnailPath = null.
    /// </summary>
    ThumbnailResult Generate(PhotoItem photo);

    /// <summary>Calea absolută pentru o cale relativă din index.</summary>
    string GetAbsolutePath(string relativePath);

    /// <summary>Șterge fișierul miniaturii (curățarea cache-ului la eliminarea din index).</summary>
    void Delete(string? relativePath);
}
