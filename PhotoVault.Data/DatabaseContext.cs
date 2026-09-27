using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;

namespace PhotoVault.Data;

/// <summary>
/// Punctul unic de acces la baza SQLite: construiește conexiunile și aplică
/// migrările (scripturi .sql incluse ca resurse) la prima pornire.
/// Versiunea schemei este ținută în <c>PRAGMA user_version</c>.
/// </summary>
public sealed partial class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(string databasePath)
    {
        DatabasePath = databasePath;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,          // necesar pentru ON DELETE CASCADE
            Cache = SqliteCacheMode.Private,
        }.ToString();
    }

    public string DatabasePath { get; }

    /// <summary>Deschide o conexiune nouă (apelantul o închide prin <c>using</c>).</summary>
    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Creează fișierul DB dacă lipsește și aplică migrările încă neaplicate.
    /// Idempotent — sigur de apelat la fiecare pornire.
    /// </summary>
    public void Initialize()
    {
        var directory = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        using var connection = OpenConnection();
        // WAL: scrieri mai rapide și citiri concurente cu indexarea de fundal
        connection.Execute("PRAGMA journal_mode = WAL;");

        var currentVersion = connection.ExecuteScalar<long>("PRAGMA user_version;");
        var pending = LoadMigrations().Where(m => m.Version > currentVersion).ToList();

        // O bază existentă care urmează să fie actualizată (versiune nouă a aplicației) → copie de siguranță înainte (§12.1)
        if (currentVersion > 0 && pending.Count > 0) new DatabaseBackup(this).CreateBackup(force: true);

        foreach (var (version, script) in pending)
        {
            using var transaction = connection.BeginTransaction();
            connection.Execute(script, transaction: transaction);
            connection.Execute($"PRAGMA user_version = {version};", transaction: transaction);
            transaction.Commit();
        }
    }

    /// <summary>Versiunea curentă a schemei (numărul ultimei migrări aplicate).</summary>
    public long GetSchemaVersion()
    {
        using var connection = OpenConnection();
        return connection.ExecuteScalar<long>("PRAGMA user_version;");
    }

    private static IEnumerable<(int Version, string Script)> LoadMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceNames()
            .Select(name => (Name: name, Match: MigrationName().Match(name)))
            .Where(x => x.Match.Success)
            .Select(x =>
            {
                using var stream = assembly.GetManifestResourceStream(x.Name)!;
                using var reader = new StreamReader(stream);
                return (int.Parse(x.Match.Groups[1].Value), reader.ReadToEnd());
            })
            .OrderBy(m => m.Item1)
            .ToList();
    }

    // Ex.: PhotoVault.Data.Migrations.001_InitialSchema.sql
    [GeneratedRegex(@"\.Migrations\._?(\d+)_[^.]*\.sql$")]
    private static partial Regex MigrationName();
}
