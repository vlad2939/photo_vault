using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;
using PhotoVault.Core.Utils;

namespace PhotoVault.Core.Services;

public sealed class BatchRenameService(IPhotoRepository photos, IDatabaseBackup? backup = null) : IBatchRenameService
{
    // Setul Windows (independent de platforma pe care rulează testele)
    private static readonly char[] InvalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    private const int MaxFileNameLength = 255;
    private const string TempPrefix = "~pvrename_";

    public IReadOnlyList<RenameSource> GetFiles(string folderPath) =>
        Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly)
            .Where(path => SupportedFormats.NormalizeExtension(path) is not null)
            .Select(path => new RenameSource(path, File.GetLastWriteTime(path)))
            .OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyList<RenamePlanItem> BuildPlan(string folderPath, IReadOnlyList<RenameSource> files, RenamePattern pattern,
        int counterStart, RenameOrder order)
    {
        var ordered = order == RenameOrder.ByDate
            ? files.OrderBy(f => f.FileDate).ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase).ToList()
            : files.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase).ToList();

        var names = ordered
            .Select((file, i) => pattern.FormatStem(file.FileName, counterStart + i, file.FileDate) + Path.GetExtension(file.FileName))
            .ToList();

        // Windows nu face diferența între majuscule și minuscule în numele de fișiere
        var targetCounts = names.GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var batchNames = new HashSet<string>(ordered.Select(f => f.FileName), StringComparer.OrdinalIgnoreCase);
        var otherFiles = Directory.Exists(folderPath)
            ? Directory.EnumerateFileSystemEntries(folderPath).Select(Path.GetFileName).OfType<string>()
                .Where(n => !batchNames.Contains(n)).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var plan = new List<RenamePlanItem>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var file = ordered[i];
            var name = names[i];
            var status =
                !IsValidFileName(name) ? RenameStatus.Invalid
                : targetCounts[name] > 1 ? RenameStatus.Duplicate
                : otherFiles.Contains(name) ? RenameStatus.Exists
                : string.Equals(name, file.FileName, StringComparison.Ordinal) ? RenameStatus.Unchanged
                : RenameStatus.Ok;
            plan.Add(new RenamePlanItem(file, name, status));
        }
        return plan;
    }

    public int CountIndexed(string folderPath) => photos.CountInFolder(folderPath);

    public RenameResult Apply(string folderPath, IReadOnlyList<RenamePlanItem> plan)
    {
        if (plan.Any(p => p.IsConflict))
            throw new BatchRenameException(RenameFailure.PlanHasConflicts);

        var moves = plan.Where(p => p.Status == RenameStatus.Ok)
            .Select(p => (From: p.Source.FullPath, To: Path.Combine(folderPath, p.NewName), Temp: Path.Combine(folderPath, TempPrefix + Guid.NewGuid().ToString("N"))))
            .ToList();
        if (moves.Count == 0) return new RenameResult(0, 0);

        // Re-verificare imediat înainte de execuție: discul se poate schimba după previzualizare
        var sources = new HashSet<string>(moves.Select(m => m.From), StringComparer.OrdinalIgnoreCase);
        foreach (var move in moves)
        {
            if (!File.Exists(move.From))
                throw new BatchRenameException(RenameFailure.SourceMissing, Path.GetFileName(move.From));
            if (File.Exists(move.To) && !sources.Contains(move.To))
                throw new BatchRenameException(RenameFailure.TargetExists, Path.GetFileName(move.To));
        }

        // §12.1: folder indexat → căile din bibliotecă vor fi rescrise
        if (photos.CountInFolder(folderPath) > 0) backup?.CreateBackup();

        // Două etape (nume temporar → nume final): permite schimburi de nume (A→B, B→A) și redenumiri doar ca majuscule
        var toTemp = new List<(string From, string Temp)>();
        var toFinal = new List<(string Temp, string To, string From)>();
        try
        {
            foreach (var move in moves)
            {
                File.Move(move.From, move.Temp);
                toTemp.Add((move.From, move.Temp));
            }
            foreach (var move in moves)
            {
                File.Move(move.Temp, move.To);
                toFinal.Add((move.Temp, move.To, move.From));
            }
        }
        catch
        {
            Rollback(toTemp, toFinal);
            throw;
        }

        // Pozele deja indexate își păstrează Id-ul → albumele, tag-urile și rotirea rămân
        var indexUpdated = photos.UpdatePaths(moves.Select(m => (m.From, m.To)).ToList());
        return new RenameResult(moves.Count, indexUpdated);
    }

    private static void Rollback(List<(string From, string Temp)> toTemp, List<(string Temp, string To, string From)> toFinal)
    {
        foreach (var (_, to, from) in toFinal.AsEnumerable().Reverse())
            TryMove(to, from);
        var finalized = toFinal.Select(f => f.Temp).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (from, temp) in toTemp.AsEnumerable().Reverse())
            if (!finalized.Contains(temp)) TryMove(temp, from);
    }

    private static void TryMove(string from, string to)
    {
        try
        {
            if (File.Exists(from) && !File.Exists(to)) File.Move(from, to);
        }
        catch (IOException)
        {
            // Cel mai bun efort — fișierul rămâne cu numele curent
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static bool IsValidFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxFileNameLength) return false;
        if (name.IndexOfAny(InvalidChars) >= 0 || name.Any(char.IsControl)) return false;
        if (name.EndsWith('.') || name.EndsWith(' ') || name.StartsWith(' ')) return false;
        var stem = Path.GetFileNameWithoutExtension(name);
        if (stem.Trim().Length == 0 || stem.StartsWith(TempPrefix, StringComparison.OrdinalIgnoreCase)) return false;
        return !ReservedNames.Contains(stem.Split('.')[0]);
    }
}
