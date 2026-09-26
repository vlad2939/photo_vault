namespace PhotoVault.Core.Abstractions;

/// <summary>
/// Acces brut cheie-valoare la tabela AppSettings. Implementat în PhotoVault.Data;
/// definit aici pentru ca serviciile din Core să nu depindă de stratul de date.
/// </summary>
public interface IAppSettingsRepository
{
    IReadOnlyDictionary<string, string?> GetAll();
    string? Get(string key);
    void Set(string key, string? value);
}
