namespace PhotoVault.Core.Models;

/// <summary>
/// Informații EXIF afișate (doar citire) în panoul de detalii (§5.6).
/// Toate câmpurile sunt opționale — lipsesc des la PNG sau la poze editate.
/// </summary>
public sealed record PhotoDetails(
    int? Width,
    int? Height,
    DateTime? DateTaken,
    string? Camera,
    string? Lens,
    string? Iso,
    string? ExposureTime,
    string? Aperture,
    string? FocalLength);
