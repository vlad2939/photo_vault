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

    /// <summary>Textul introdus (doar în modul Prompt).</summary>
    public string InputText => InputBox.Text.Trim();

    /// <summary>Textul din câmpul secundar (subtitlu), dacă a fost activat.</summary>
    public string SecondaryText => SecondaryBox.Text.Trim();

    private bool HasInput => InputPanel.Visibility == Visibility.Visible;

    /// <summary>Transformă dialogul într-un Prompt cu câmp text (OK activ doar pentru text negol).</summary>
    public void EnableInput(string initialText, string placeholder, string? label = null)
    {
        InputPanel.Visibility = Visibility.Visible;
        InputBox.Text = initialText;
        Controls.InputHelper.SetPlaceholder(InputBox, placeholder);
        if (label is not null)
        {
            InputLabel.Text = label;
            InputLabel.Visibility = Visibility.Visible;
        }
        InputBox.SelectAll();
        OnInputChanged(InputBox, null!);
    }

    /// <summary>Al doilea câmp, opțional (ex. subtitlul albumului).</summary>
    public void EnableSecondaryInput(string label, string initialText, string placeholder)
    {
        SecondaryLabel.Text = label;
        SecondaryLabel.Visibility = Visibility.Visible;
        SecondaryBox.Visibility = Visibility.Visible;
        SecondaryBox.Text = initialText;
        Controls.InputHelper.SetPlaceholder(SecondaryBox, placeholder);
    }

    /// <summary>Etichete proprii pentru butoane (ex. „Modifică numele" / „Renunță").</summary>
    public void SetButtonTexts(string? primary, string? secondary)
    {
        if (primary is not null) PrimaryButton.Content = primary;
        if (secondary is not null) SecondaryButton.Content = secondary;
    }

    private void OnInputChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) =>
        PrimaryButton.IsEnabled = !HasInput || InputText.Length > 0;

    /// <summary>
    /// Poziționează dialogul peste zona client a ferestrei părinte (overlay).
    /// Fără părinte: fereastră compactă, centrată pe ecran, fără overlay.
    /// </summary>
    public void AttachTo(Window? owner)
    {
        if (OverlayPlacement.Cover(this, owner)) return;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        SizeToContent = SizeToContent.WidthAndHeight;
        Overlay.Visibility = Visibility.Collapsed;
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
        var scaleY = new DoubleAnimation(1, AnimationDuration) { EasingFunction = ease };
        // Focus după animație: altfel conturul de focus e poziționat pe cardul încă scalat.
        scaleY.Completed += (_, _) =>
        {
            if (HasInput) InputBox.Focus(); else PrimaryButton.Focus();
        };
        CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, AnimationDuration) { EasingFunction = ease });
        CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
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
