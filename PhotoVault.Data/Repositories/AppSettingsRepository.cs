using Dapper;
using PhotoVault.Core.Abstractions;

namespace PhotoVault.Data.Repositories;

/// <summary>Acces cheie-valoare la tabela AppSettings.</summary>
public sealed class AppSettingsRepository(DatabaseContext db) : IAppSettingsRepository
{
    public IReadOnlyDictionary<string, string?> GetAll()
    {
        using var connection = db.OpenConnection();
        return connection.Query<(string Key, string? Value)>("SELECT Key, Value FROM AppSettings")
            .ToDictionary(r => r.Key, r => r.Value);
    }

    public string? Get(string key)
    {
        using var connection = db.OpenConnection();
        return connection.ExecuteScalar<string?>("SELECT Value FROM AppSettings WHERE Key = @key", new { key });
    }

    public void Set(string key, string? value)
    {
        using var connection = db.OpenConnection();
        connection.Execute(
            """
            INSERT INTO AppSettings (Key, Value) VALUES (@key, @value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value
            """,
            new { key, value });
    }
}
