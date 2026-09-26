using PhotoVault.Core.Models;

namespace PhotoVault.App.Utils;

/// <summary>Abstracția folosită de ViewModel-uri pentru a schimba tema.</summary>
public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    void SetTheme(AppTheme theme);
    event EventHandler? ThemeChanged;
}
