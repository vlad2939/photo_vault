using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PhotoVault.App.Converters;

/// <summary>Visible dacă valoarea enum-ului e egală cu parametrul (ex. ConverterParameter=Photo).</summary>
public sealed class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() == parameter as string ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
