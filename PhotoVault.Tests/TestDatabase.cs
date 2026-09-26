using PhotoVault.Data;

namespace PhotoVault.Tests;

/// <summary>Bază de date SQLite temporară, ștearsă la final.</summary>
internal sealed class TestDatabase : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "PhotoVaultTests", Guid.NewGuid().ToString("N"));

    public TestDatabase()
    {
        Context = new DatabaseContext(Path.Combine(_directory, "data", "photovault.db"));
        Context.Initialize();
    }

    public DatabaseContext Context { get; }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }
}
