using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PhotoVault.App.Utils;

namespace PhotoVault.App.Views;

public partial class InfoWindow : Window
{
    private bool _closing;

    public InfoWindow()
    {
        InitializeComponent();
        VersionText.Text = Loc.Format("Str.Info.Version", AppVersion());
        HowToList.ItemsSource = Loc.Get("Str.Info.HowToText").Split('|', StringSplitOptions.TrimEntries);
        ShortcutList.ItemsSource = Loc.Get("Str.Info.ShortcutsText").Split('|', StringSplitOptions.TrimEntries)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim()))
            .ToList();
        Loaded += (_, _) => Animate(true);
    }

    /// <summary>Overlay peste fereastra părinte; fără părinte → centrat pe ecran.</summary>
    public void AttachTo(Window? owner)
    {
        if (OverlayPlacement.Cover(this, owner)) return;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        SizeToContent = SizeToContent.WidthAndHeight;
        Overlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>Versiunea din assembly (fără sufixul de commit adăugat de SDK).</summary>
    private static string AppVersion()
    {
        var info = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return info?.Split('+')[0] ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        CloseAnimated();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => CloseAnimated();

    private void OnOverlayClick(object sender, MouseButtonEventArgs e) => CloseAnimated();

    private void CloseAnimated()
    {
        if (_closing) return;
        _closing = true;
        Animate(false);
    }

    private void Animate(bool show)
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(show ? 160 : 110));
        var target = show ? 1.0 : 0.0;
        var fade = new DoubleAnimation(target, duration);
        if (!show) fade.Completed += (_, _) => Close();
        Overlay.BeginAnimation(OpacityProperty, new DoubleAnimation(target, duration));
        Card.BeginAnimation(OpacityProperty, fade);
        var scale = show ? 1.0 : 0.97;
        CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(scale, duration));
        CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(scale, duration));
    }
}
