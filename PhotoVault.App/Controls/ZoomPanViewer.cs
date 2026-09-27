using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoVault.App.Controls;

/// <summary>
/// Afișare imagine cu zoom (rotița mouse-ului, centrat pe cursor / butoane) și pan (drag)
/// când zoom-ul depășește „potrivire în ecran" (§6.4). Zoom = 1 înseamnă imaginea încadrată integral.
/// </summary>
public class ZoomPanViewer : Border
{
    private const double MaxZoomOverActual = 8;
    private const double Step = 1.25;

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(ImageSource), typeof(ZoomPanViewer),
        new PropertyMetadata(null, (d, e) => ((ZoomPanViewer)d).OnSourceChanged((ImageSource?)e.NewValue)));

    public static readonly DependencyProperty ZoomPercentProperty = DependencyProperty.Register(
        nameof(ZoomPercent), typeof(double), typeof(ZoomPanViewer), new PropertyMetadata(100.0));

    private readonly Image _image = new() { Stretch = Stretch.Uniform, RenderTransformOrigin = new Point(0, 0) };
    private readonly ScaleTransform _scale = new();
    private readonly TranslateTransform _translate = new();
    private Point? _dragStart;
    private Point _dragOrigin;

    public ZoomPanViewer()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;   // captează mouse-ul și în zonele goale
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.HighQuality);
        _image.RenderTransform = new TransformGroup { Children = { _scale, _translate } };
        Child = _image;
        SizeChanged += (_, _) => ClampAndUpdate();
    }

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Zoom-ul raportat la pixelii reali ai imaginii (100% = 1:1). Pentru afișare în UI.</summary>
    public double ZoomPercent
    {
        get => (double)GetValue(ZoomPercentProperty);
        private set => SetValue(ZoomPercentProperty, value);
    }

    public double Zoom => _scale.ScaleX;
    public bool IsZoomed => Zoom > 1.001;

    public void ZoomIn() => ZoomAt(new Point(ActualWidth / 2, ActualHeight / 2), Zoom * Step);
    public void ZoomOut() => ZoomAt(new Point(ActualWidth / 2, ActualHeight / 2), Zoom / Step);

    /// <summary>Potrivire în ecran (imaginea întreagă vizibilă).</summary>
    public void Fit()
    {
        _scale.ScaleX = _scale.ScaleY = 1;
        _translate.X = _translate.Y = 0;
        ClampAndUpdate();
    }

    /// <summary>1:1 — un pixel al imaginii pe un pixel al ecranului.</summary>
    public void ActualSize() => ZoomAt(new Point(ActualWidth / 2, ActualHeight / 2), ActualSizeZoom());

    private void OnSourceChanged(ImageSource? source)
    {
        _image.Source = source;
        Fit();
    }

    private void ZoomAt(Point anchor, double zoom)
    {
        zoom = Math.Clamp(zoom, 1, Math.Max(1, ActualSizeZoom() * MaxZoomOverActual));
        var ratio = zoom / Zoom;
        // Punctul de sub cursor rămâne pe loc: t' = p − (p − t) · raport
        _translate.X = anchor.X - (anchor.X - _translate.X) * ratio;
        _translate.Y = anchor.Y - (anchor.Y - _translate.Y) * ratio;
        _scale.ScaleX = _scale.ScaleY = zoom;
        ClampAndUpdate();
    }

    /// <summary>Zoom-ul la care imaginea e afișată la pixelii ei reali (ține cont de DPI).</summary>
    private double ActualSizeZoom()
    {
        if (_image.Source is not BitmapSource bitmap || ActualWidth <= 0 || ActualHeight <= 0) return 1;
        var dpi = VisualTreeHelper.GetDpi(this);
        var imageWidth = bitmap.PixelWidth / dpi.DpiScaleX;
        var imageHeight = bitmap.PixelHeight / dpi.DpiScaleY;
        var fitScale = Math.Min(ActualWidth / imageWidth, ActualHeight / imageHeight);
        return fitScale <= 0 ? 1 : 1 / fitScale;
    }

    /// <summary>Păstrează imaginea în cadru: nu se poate trage dincolo de margini; centrată cât timp încape.</summary>
    private void ClampAndUpdate()
    {
        var zoom = Zoom;
        _translate.X = ClampAxis(_translate.X, ActualWidth, ActualWidth * zoom);
        _translate.Y = ClampAxis(_translate.Y, ActualHeight, ActualHeight * zoom);
        ZoomPercent = Math.Round(zoom / ActualSizeZoom() * 100);
        Cursor = IsZoomed ? (_dragStart is null ? Cursors.Hand : Cursors.SizeAll) : null;
    }

    private static double ClampAxis(double offset, double viewport, double content) =>
        content <= viewport ? (viewport - content) / 2 : Math.Clamp(offset, viewport - content, 0);

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        ZoomAt(e.GetPosition(this), e.Delta > 0 ? Zoom * Step : Zoom / Step);
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ClickCount == 2)
        {
            // Dublu-click: comută între „potrivire" și 1:1 în punctul indicat
            if (IsZoomed) Fit(); else ZoomAt(e.GetPosition(this), ActualSizeZoom());
            e.Handled = true;
            return;
        }
        if (!IsZoomed) return;
        _dragStart = e.GetPosition(this);
        _dragOrigin = new Point(_translate.X, _translate.Y);
        CaptureMouse();
        ClampAndUpdate();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragStart is not { } start) return;
        var position = e.GetPosition(this);
        _translate.X = _dragOrigin.X + position.X - start.X;
        _translate.Y = _dragOrigin.Y + position.Y - start.Y;
        ClampAndUpdate();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragStart is null) return;
        _dragStart = null;
        ReleaseMouseCapture();
        ClampAndUpdate();
    }
}
