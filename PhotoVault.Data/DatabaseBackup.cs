using System.Globalization;
using Microsoft.Data.Sqlite;
using PhotoVault.Core.Abstractions;

namespace PhotoVault.Data;

/// <summary>
/// Copii de siguranță prin API-ul de backup SQLite (nu simplu File.Copy): baza e în modul WAL, iar backup-ul
/// produce un fișier complet și consistent chiar dacă aplicația scrie în acel moment.
/// </summary>
public sealed class DatabaseBackup(DatabaseContext db, int maxBackups = 5, TimeSpan? minInterval = null) : IDatabaseBackup
{
    private const string TimestampFormat = "yyyyMMdd-HHmmss-fff";
    private readonly TimeSpan _minInterval = minInterval ?? TimeSpan.FromMinutes(1);
    private readonly Lock _lock = new();

    public int MaxBackups { get; } = maxBackups;

    private string Directory => Path.GetDirectoryName(db.DatabasePath)!;
    private string Prefix => Path.GetFileName(db.DatabasePath) + ".bak.";

    public BackupInfo? CreateBackup(bool force = false)
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(db.DatabasePath)) return null;
                var latest = GetBackups().FirstOrDefault();
                if (!force && latest is not null && DateTime.Now - latest.Created < _minInterval) return null;

                var now = DateTime.Now;
                var target = Path.Combine(Directory, Prefix + now.ToString(TimestampFormat, CultureInfo.InvariantCulture));
                // Marcaj la milisecundă: numele cresc monoton, deci rotația șterge mereu cele mai vechi copii
                while (File.Exists(target))
                {
                    now = now.AddMilliseconds(1);
                    target = Path.Combine(Directory, Prefix + now.ToString(TimestampFormat, CultureInfo.InvariantCulture));
                }

                using (var source = db.OpenConnection())
                using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder
                       {
                           DataSource = target,
                           Pooling = false,
                       }.ToString()))
                {
                    destination.Open();
                    source.BackupDatabase(destination);
                    // Copia e un singur fișier de sine stătător (fără -wal / -shm), gata de restaurat prin redenumire
                    using var command = destination.CreateCommand();
                    command.CommandText = "PRAGMA journal_mode = DELETE;";
                    command.ExecuteNonQuery();
                }

                Rotate();
                var info = new FileInfo(target);
                return new BackupInfo(target, now, info.Length);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or SqliteException)
            {
                // Copia de siguranță e o plasă de protecție: eșecul ei nu blochează operațiunea utilizatorului
                return null;
            }
        }
    }

    public IReadOnlyList<BackupInfo> GetBackups()
    {
        if (!System.IO.Directory.Exists(Directory)) return [];
        return System.IO.Directory.EnumerateFiles(Directory, Prefix + "*")
            .Select(path => (path, created: ParseTimestamp(Path.GetFileName(path))))
            .Where(b => b.created is not null)
            .Select(b => new BackupInfo(b.path, b.created!.Value, new FileInfo(b.path).Length))
            .OrderByDescending(b => b.Created)
            .ToList();
    }

    private void Rotate()
    {
        foreach (var old in GetBackups().Skip(MaxBackups))
        {
            try
            {
                File.Delete(old.Path);
            }
            catch (IOException)
            {
                // Fișier blocat momentan (ex. deschis în alt program) → se reîncearcă la următoarea copie
            }
        }
    }

    private DateTime? ParseTimestamp(string fileName)
    {
        if (!fileName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var stamp = fileName[Prefix.Length..];
        // Doar numele exacte „…bak.20260927-183012-345" (nu fișiere auxiliare -wal / -shm / -journal)
        if (stamp.Length != TimestampFormat.Length) return null;
        return DateTime.TryParseExact(stamp, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : null;
    }
}
