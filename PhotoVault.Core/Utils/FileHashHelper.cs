using System.Security.Cryptography;
using System.Text;

namespace PhotoVault.Core.Utils;

/// <summary>
/// Hash-ul căii complete a unui fișier, folosit ca nume de miniatură (§6.3):
/// data/thumbnails/{primele 2 caractere}/{hash}.jpg
/// </summary>
public static class FileHashHelper
{
    /// <summary>SHA-1 hex (lowercase) al căii normalizate. Căile Windows nu țin cont de majuscule.</summary>
    public static string HashPath(string fullPath)
    {
        var normalized = Path.GetFullPath(fullPath).ToUpperInvariant();
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>Calea relativă a miniaturii, cu separator '/' (independentă de platformă).</summary>
    public static string ThumbnailRelativePath(string fullPath)
    {
        var hash = HashPath(fullPath);
        return $"{hash[..2]}/{hash}.jpg";
    }
}
