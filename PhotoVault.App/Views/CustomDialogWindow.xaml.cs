using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PhotoVault.App.Utils;

namespace PhotoVault.App.Views;

public partial class CustomDialogWindow : Window
{
    private static readonly Duration AnimationDuration = new(TimeSpan.FromMilliseconds(150));

    private readonly DialogButtons _buttons;
    private bool _closing;

    public CustomDialogWindow(string title, string message, DialogKind kind, DialogButtons buttons)
    {
        InitializeComponent();
        _buttons = buttons;

        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ApplyKind(kind);
        ApplyButtons(buttons);

        Loaded += (_, _) => AnimateIn();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public DialogResultKind Result { get; private set; } = DialogResultKind.None;

    /// <summary>
    /// Poziționează dialogul peste zona client a ferestrei părinte (overlay).
    /// Fără părinte: fereastră compactă, centrată pe ecran, fără overlay.
    /// </summary>
    public void AttachTo(Window? owner)
    {
        if (owner?.Content is FrameworkElement content && PresentationSource.FromVisual(content) is { CompositionTarget: not null } source)
        {
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.Manual;
            var topLeft = source.CompositionTarget.TransformFromDevice.Transform(content.PointToScreen(new Point(0, 0)));
            Left = topLeft.X;
            Top = topLeft.Y;
            Width = content.ActualWidth;
            Height = content.ActualHeight;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SizeToContent = SizeToContent.WidthAndHeight;
            Overlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyKind(DialogKind kind)
    {
        var (brushKey, geometry) = kind switch
        {
            DialogKind.Success => ("Brush.Status.Success", "M 11.5,18.5 L 16,23 L 24.5,13.5"),
            DialogKind.Warning => ("Brush.Status.Warning", "M 18,10.5 L 18,19.5 M 18,25 L 18,25.2"),
            DialogKind.Error => ("Brush.Status.Error", "M 13.5,13.5 L 22.5,22.5 M 22.5,13.5 L 13.5,22.5"),
            _ => ("Brush.Status.Info", "M 18,10.8 L 18,11 M 18,16 L 18,25"),
        };
        IconBadge.SetResourceReference(Shape.FillProperty, brushKey);
        IconSymbol.Data = Geometry.Parse(geometry);
    }

    private void ApplyButtons(DialogButtons buttons)
    {
        switch (buttons)
        {
            case DialogButtons.OkCancel:
                PrimaryButton.Content = Loc.Get("Str.Dialog.Ok");
                SecondaryButton.Content = Loc.Get("Str.Dialog.Cancel");
                SecondaryButton.IsCancel = true;
                break;
            case DialogButtons.YesNo:
                PrimaryButton.Content = Loc.Get("Str.Dialog.Yes");
                SecondaryButton.Content = Loc.Get("Str.Dialog.No");
                SecondaryButton.IsCancel = true;
                break;
            default:
                PrimaryButton.Content = Loc.Get("Str.Dialog.Ok");
                SecondaryButton.Visibility = Visibility.Collapsed;
                PrimaryButton.IsCancel = true;   // Esc închide și dialogul cu un singur buton
                break;
        }
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e) =>
        CloseWith(_buttons == DialogButtons.YesNo ? DialogResultKind.Yes : DialogResultKind.Ok);

    private void OnSecondaryClick(object sender, RoutedEventArgs e) =>
        CloseWith(_buttons == DialogButtons.YesNo ? DialogResultKind.No : DialogResultKind.Cancel);

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        CloseWith(_buttons switch
        {
            DialogButtons.Ok => DialogResultKind.Ok,
            DialogButtons.YesNo => DialogResultKind.No,
            _ => DialogResultKind.Cancel,
        });
    }

    private void AnimateIn()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        Overlay.BeginAnimation(OpacityProperty, new DoubleAnimation(1, AnimationDuration));
        Card.BeginAnimation(OpacityProperty, new DoubleAnimation(1, AnimationDuration));
        CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, AnimationDuration) { EasingFunction = ease });
        CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, AnimationDuration) { EasingFunction = ease });
        PrimaryButton.Focus();
    }

    /// <summary>Fade-out scurt, apoi închidere.</summary>
    private void CloseWith(DialogResultKind result)
    {
        if (_closing) return;
        _closing = true;
        Result = result;

        var fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(110)));
        fade.Completed += (_, _) => Close();
        Overlay.BeginAnimation(OpacityProperty, new DoubleAnimation(0, fade.Duration));
        CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.97, fade.Duration));
        CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.97, fade.Duration));
        Card.BeginAnimation(OpacityProperty, fade);
    }
}
