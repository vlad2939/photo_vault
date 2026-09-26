using System.Globalization;
using System.Windows.Data;

namespace PhotoVault.App.Converters;

/// <summary>bool → !bool (ex. radio „Temă luminoasă" legat de IsDarkTheme).</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}
