using System.Windows;

namespace PhotoVault.App.Controls;

/// <summary>
/// Proprietăți atașate folosite de template-urile de input (TextBox):
/// text placeholder și iconiță opțională în dreapta câmpului.
/// </summary>
public static class InputHelper
{
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached("Placeholder", typeof(string), typeof(InputHelper),
            new FrameworkPropertyMetadata(string.Empty));

    public static string GetPlaceholder(DependencyObject d) => (string)d.GetValue(PlaceholderProperty);
    public static void SetPlaceholder(DependencyObject d, string value) => d.SetValue(PlaceholderProperty, value);

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.RegisterAttached("Icon", typeof(string), typeof(InputHelper),
            new FrameworkPropertyMetadata(string.Empty));

    public static string GetIcon(DependencyObject d) => (string)d.GetValue(IconProperty);
    public static void SetIcon(DependencyObject d, string value) => d.SetValue(IconProperty, value);
}
