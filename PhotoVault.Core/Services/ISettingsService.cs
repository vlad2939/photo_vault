using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

/// <summary>Încarcă și salvează setările tipizate ale aplicației.</summary>
public interface ISettingsService
{
    /// <summary>Setările curente (încărcate la pornire).</summary>
    AppSettings Current { get; }

    /// <summary>Reîncarcă setările din baza de date.</summary>
    AppSettings Load();

    /// <summary>Persistă toate setările curente.</summary>
    void Save();

    /// <summary>Schimbă tema și o persistă imediat.</summary>
    void SetTheme(AppTheme theme);
}
