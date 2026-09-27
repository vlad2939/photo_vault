using System.Windows;
using System.Windows.Controls;
using PhotoVault.App.Controls;

namespace PhotoVault.App.Views;

public partial class BatchRenameWindow : ThemedWindow
{
    public BatchRenameWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            PatternBox.Focus();
            PatternBox.CaretIndex = PatternBox.Text.Length;
        };
    }

    /// <summary>Butoanele-jeton inserează variabila la poziția cursorului din câmpul pattern.</summary>
    private void OnTokenClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button { Tag: string token }) return;
        var caret = PatternBox.SelectionStart;
        var text = PatternBox.Text.Remove(caret, PatternBox.SelectionLength).Insert(caret, token);
        PatternBox.Text = text;
        PatternBox.Focus();
        PatternBox.CaretIndex = caret + token.Length;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
