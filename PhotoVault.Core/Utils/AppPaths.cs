namespace PhotoVault.Core.Utils;

/// <summary>
/// Căile aplicației portabile. Totul se află lângă executabil (§3.2):
/// data/photovault.db, data/thumbnails/, logs/.
/// </summary>
public sealed class AppPaths
{
    public AppPaths(string? baseDirectory = null)
    {
        BaseDirectory = baseDirectory ?? AppContext.BaseDirectory;
        DataDirectory = Path.Combine(BaseDirectory, "data");
        DatabasePath = Path.Combine(DataDirectory, "photovault.db");
        ThumbnailsDirectory = Path.Combine(DataDirectory, "thumbnails");
        LogsDirectory = Path.Combine(BaseDirectory, "logs");
    }

    public string BaseDirectory { get; }
    public string DataDirectory { get; }
    public string DatabasePath { get; }
    public string ThumbnailsDirectory { get; }
    public string LogsDirectory { get; }

    /// <summary>Creează folderele de date dacă nu există (prima rulare).</summary>
    public void EnsureCreated()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(ThumbnailsDirectory);
    }
}
