using System.IO;
using System.Text;

namespace PhotoVault.App.Utils;

/// <summary>
/// Log de erori în folderul logs/ de lângă executabil (§3.2), un fișier pe zi,
/// rotativ — se păstrează doar ultimele <see cref="MaxFiles"/> fișiere.
/// </summary>
public static class ErrorLog
{
    private const int MaxFiles = 5;
    private static readonly object Sync = new();
    private static string? _directory;

    public static void Initialize(string logsDirectory) => _directory = logsDirectory;

    public static void Write(Exception exception, string context)
    {
        if (_directory is null) return;
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(_directory);
                var file = Path.Combine(_directory, $"photovault-{DateTime.Now:yyyyMMdd}.log");
                var entry = new StringBuilder()
                    .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}")
                    .AppendLine(exception.ToString())
                    .AppendLine();
                File.AppendAllText(file, entry.ToString(), Encoding.UTF8);
                Rotate();
            }
        }
        catch (IOException) { /* logarea nu trebuie să producă la rândul ei erori */ }
        catch (UnauthorizedAccessException) { }
    }

    private static void Rotate()
    {
        var old = new DirectoryInfo(_directory!).GetFiles("photovault-*.log")
            .OrderByDescending(f => f.Name)
            .Skip(MaxFiles);
        foreach (var file in old) file.Delete();
    }
}
