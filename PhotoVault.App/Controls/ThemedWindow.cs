using System.Windows;
using System.Windows.Media;
using PhotoVault.App.Utils;

namespace PhotoVault.App.Controls;

/// <summary>
/// Clasă de bază pentru toate ferestrele cu title bar nativ ale aplicației:
/// fundal/text/font din tema activă + title bar sincronizat prin DWM.
/// Ferestrele noi (Setări, Lightbox, Redenumire batch) moștenesc automat stilul.
/// </summary>
public class ThemedWindow : Window
{
    public ThemedWindow()
    {
        SetResourceReference(BackgroundProperty, "Brush.Window.Background");
        SetResourceReference(ForegroundProperty, "Brush.Text.Primary");
        SetResourceReference(FontFamilyProperty, "Font.Text");
        SetResourceReference(FontSizeProperty, "FontSize.Body");
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Înainte de primul frame afișat → title bar-ul apare direct în culoarea temei.
        ThemeManager.Instance.UpdateTitleBar(this);
    }
}
