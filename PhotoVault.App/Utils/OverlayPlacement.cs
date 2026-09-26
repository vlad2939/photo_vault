using System.Windows;

namespace PhotoVault.App.Utils;

/// <summary>Poziționează o fereastră fără chrome exact peste zona client a ferestrei părinte (overlay modal).</summary>
public static class OverlayPlacement
{
    /// <returns>false dacă nu există o fereastră părinte vizibilă (overlay-ul nu poate fi aplicat).</returns>
    public static bool Cover(Window overlay, Window? owner)
    {
        if (owner?.Content is not FrameworkElement content ||
            PresentationSource.FromVisual(content) is not { CompositionTarget: not null } source)
            return false;

        overlay.Owner = owner;
        overlay.WindowStartupLocation = WindowStartupLocation.Manual;
        var topLeft = source.CompositionTarget.TransformFromDevice.Transform(content.PointToScreen(new Point(0, 0)));
        overlay.Left = topLeft.X;
        overlay.Top = topLeft.Y;
        overlay.Width = content.ActualWidth;
        overlay.Height = content.ActualHeight;
        return true;
    }
}
