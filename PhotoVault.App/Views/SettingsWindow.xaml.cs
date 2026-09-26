using System.Windows;
using PhotoVault.App.Controls;

namespace PhotoVault.App.Views;

public partial class SettingsWindow : ThemedWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
