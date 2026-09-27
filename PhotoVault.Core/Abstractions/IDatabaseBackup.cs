namespace PhotoVault.Core.Abstractions;

/// <summary>O copie de siguranță a bazei de date (data/photovault.db.bak.{timestamp}).</summary>
public sealed record BackupInfo(string Path, DateTime Created, long SizeBytes);

/// <summary>
/// Copii de siguranță automate ale bazei de date (§12.1), create înainte de operațiunile cu potențial distructiv
/// asupra organizării (ștergere album / tag / folder sursă, eliminarea pozelor lipsă, redenumire batch pe un folder
/// indexat, schimbarea locației unui folder). Se păstrează doar ultimele copii (rotație).
/// </summary>
public interface IDatabaseBackup
{
    /// <summary>
    /// Creează o copie consistentă a bazei (inclusiv modificările din jurnalul WAL). Dacă ultima copie are mai puțin de
    /// un minut, nu se creează alta (ea reflectă deja starea dinaintea operațiunilor în lanț). Nu aruncă excepții:
    /// întoarce null dacă nu s-a creat nicio copie.
    /// </summary>
    BackupInfo? CreateBackup(bool force = false);

    /// <summary>Copiile existente, cea mai nouă prima.</summary>
    IReadOnlyList<BackupInfo> GetBackups();

    /// <summary>Numărul maxim de copii păstrate.</summary>
    int MaxBackups { get; }
}
