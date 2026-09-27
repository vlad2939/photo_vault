using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace PhotoVault.App.Controls;

/// <summary>
/// Auto-hide pentru elementele UI suprapuse peste poză (§5.8): după 3 s fără mișcare de mouse, grupul dispare
/// (fade 350 ms, cursor ascuns); reapare instant la mișcare. Folosit de vizualizarea pe tot ecranul și de slideshow.
/// </summary>
public sealed class ChromeAutoHide
{
    private static readonly Duration FadeOut = new(TimeSpan.FromMilliseconds(350));
    private static readonly Duration FadeIn = new(TimeSpan.FromMilliseconds(150));

    private readonly Window _window;
    private readonly UIElement _chrome;
    private readonly FrameworkElement[] _keepVisibleWhileHovered;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(3) };
    private Point? _lastMouse;

    public ChromeAutoHide(Window window, UIElement chrome, params FrameworkElement[] keepVisibleWhileHovered)
    {
        _window = window;
        _chrome = chrome;
        _keepVisibleWhileHovered = keepVisibleWhileHovered;
        _timer.Tick += (_, _) => Hide();
        window.PreviewMouseMove += OnMouseMove;
        window.Loaded += (_, _) => _timer.Start();
        window.Closed += (_, _) => _timer.Stop();
    }

    public bool IsVisible { get; private set; } = true;

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        // WPF trimite MouseMove și când conținutul se schimbă sub cursor → contează doar mișcarea reală
        var position = e.GetPosition(_window);
        if (_lastMouse is { } last && Math.Abs(last.X - position.X) < 2 && Math.Abs(last.Y - position.Y) < 2) return;
        _lastMouse = position;
        Show();
    }

    /// <summary>Afișează elementele (ex. la o acțiune din tastatură) și repornește numărătoarea de 3 s.</summary>
    public void Show()
    {
        _timer.Stop();
        _timer.Start();
        _window.ClearValue(FrameworkElement.CursorProperty);
        if (IsVisible) return;
        IsVisible = true;
        _chrome.IsHitTestVisible = true;
        _chrome.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, FadeIn));
    }

    private void Hide()
    {
        _timer.Stop();
        // Cât timp mouse-ul stă pe bara de control (sau un buton e apăsat), elementele rămân vizibile
        if (_keepVisibleWhileHovered.Any(e => e.IsMouseOver) || Mouse.LeftButton == MouseButtonState.Pressed)
        {
            _timer.Start();
            return;
        }
        IsVisible = false;
        _chrome.IsHitTestVisible = false;
        _chrome.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, FadeOut));
        _window.Cursor = Cursors.None;
    }
}
