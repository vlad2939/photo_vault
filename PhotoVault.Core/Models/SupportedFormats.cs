namespace PhotoVault.Core.Models;

/// <summary>Formatele indexate (§2): JPEG, PNG și RAW (CR2, NEF, DNG).</summary>
public static class SupportedFormats
{
    private static readonly HashSet<string> Raw = new(StringComparer.OrdinalIgnoreCase) { "cr2", "nef", "dng" };

    private static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "cr2", "nef", "dng"
    };

    /// <summary>Extensia normalizată (fără punct, lowercase) sau null dacă fișierul nu e suportat.</summary>
    public static string? NormalizeExtension(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return All.Contains(ext) ? ext : null;
    }

    public static bool IsRaw(string extension) => Raw.Contains(extension);
}
