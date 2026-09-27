using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using PhotoVault.App.Utils;

namespace PhotoVault.App.Views;

public partial class LogoWindow : Window
{
    private bool _closing;

    public LogoWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Animate(1, 1);
        SizeChanged += (_, _) =>
        {
            // „Dimensiune generoasă": ~72% din latura mică a zonei, fără a depăși rezoluția sursei
            var side = Math.Min(Math.Min(ActualWidth, ActualHeight) * 0.72, 1024);
            Logo.Width = Logo.Height = side;
        };
    }

    /// <summary>Acoperă fereastra părinte și folosește un instantaneu al ei ca fundal blurat.</summary>
    public void AttachTo(Window? owner)
    {
        if (owner?.Content is FrameworkElement content && content.ActualWidth > 0)
        {
            var dpi = VisualTreeHelper.GetDpi(content);
            var snapshot = new RenderTargetBitmap(
                (int)Math.Ceiling(content.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(content.ActualHeight * dpi.DpiScaleY),
                dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            snapshot.Render(content);
            snapshot.Freeze();
            Backdrop.Source = snapshot;
        }

        if (!OverlayPlacement.Cover(this, owner))
        {
            WindowState = WindowState.Maximized;
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        CloseAnimated();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        CloseAnimated();
    }

    private void CloseAnimated()
    {
        if (_closing) return;
        _closing = true;
        Animate(0, 0.95, Close);
    }

    private void Animate(double opacity, double scale, Action? completed = null)
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(opacity > 0 ? 220 : 140));
        var fade = new DoubleAnimation(opacity, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        if (completed is not null) fade.Completed += (_, _) => completed();
        Root.BeginAnimation(OpacityProperty, fade);
        LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(scale, duration));
        LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(scale, duration));
    }
}
