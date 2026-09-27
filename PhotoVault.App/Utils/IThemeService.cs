using PhotoVault.Core.Models;

namespace PhotoVault.App.Utils;

/// <summary>Abstracția folosită de ViewModel-uri pentru a schimba tema.</summary>
public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    void SetTheme(AppTheme theme);

    /// <summary>Culoarea de accent (#RRGGBB), aplicată imediat în toată aplicația, adaptată temei.</summary>
    void SetAccent(string accentHex);
    event EventHandler? ThemeChanged;
}
