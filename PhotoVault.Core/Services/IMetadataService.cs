namespace PhotoVault.Core.Services;

/// <summary>Metadatele EXIF de care are nevoie aplicația.</summary>
/// <param name="DateTaken">DateTimeOriginal (sau DateTime din IFD0), dacă există.</param>
/// <param name="Orientation">Orientarea EXIF 1–8 (1 = normal).</param>
public readonly record struct PhotoMetadata(DateTime? DateTaken, int Orientation);

/// <summary>Wrapper peste MetadataExtractor (§3.3).</summary>
public interface IMetadataService
{
    /// <summary>Citește metadatele; la fișiere ilizibile întoarce valori implicite.</summary>
    PhotoMetadata Read(string path);
}
