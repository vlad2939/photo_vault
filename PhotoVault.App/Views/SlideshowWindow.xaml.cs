using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PhotoVault.App.Controls;
using PhotoVault.App.ViewModels;

namespace PhotoVault.App.Views;

/// <summary>
/// Animațiile slideshow-ului (§6.10): Ken Burns (ScaleTransform + TranslateTransform) pe stratul nou,
/// fade încrucișat între straturi; elementele UI se ascund automat (<see cref="ChromeAutoHide"/>, §5.8).
/// </summary>
public partial class SlideshowWindow : Window
{
    private readonly ChromeAutoHide _chrome;
    private readonly Dictionary<Image, List<AnimationClock>> _motionClocks = [];
    private Image _front;

    public SlideshowWindow()
    {
        InitializeComponent();
        _front = LayerB;   // primul slide intră pe LayerA
        foreach (var layer in new[] { LayerA, LayerB })
        {
            layer.RenderTransform = new TransformGroup { Children = { new ScaleTransform(), new TranslateTransform() } };
            _motionClocks[layer] = [];
        }

        _chrome = new ChromeAutoHide(this, Chrome, ControlBar);
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => ViewModel?.Start();
    }

    private SlideshowViewModel? ViewModel => DataContext as SlideshowViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SlideshowViewModel old)
        {
            old.SlideReady -= OnSlideReady;
            old.CloseRequested -= Close;
            old.PropertyChanged -= OnViewModelPropertyChanged;
        }
        if (e.NewValue is SlideshowViewModel vm)
        {
            vm.SlideReady += OnSlideReady;
            vm.CloseRequested += Close;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    // ------------------------------------------------------------------ Poze

    private void OnSlideReady(SlideshowSlide slide)
    {
        if (ViewModel is not { } vm) return;
        var incoming = _front == LayerA ? LayerB : LayerA;
        var outgoing = _front;
        _front = incoming;

        StopMotion(incoming);
        incoming.Source = slide.Image;
        Panel.SetZIndex(incoming, 1);
        Panel.SetZIndex(outgoing, 0);

        // Fade încrucișat: stratul nou apare peste cel vechi, care dispare simultan
        var fade = new Duration(vm.FadeDuration);
        incoming.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, fade));
        outgoing.BeginAnimation(OpacityProperty, new DoubleAnimation(0, fade));

        StartMotion(incoming, slide, vm.MotionDuration);
        if (!vm.IsPlaying) PauseMotion(incoming);
    }

    private void StartMotion(Image layer, SlideshowSlide slide, TimeSpan duration)
    {
        var group = (TransformGroup)layer.RenderTransform;
        var scale = (ScaleTransform)group.Children[0];
        var translate = (TranslateTransform)group.Children[1];
        var m = slide.Motion;
        var width = Math.Max(1, Stage.ActualWidth);
        var height = Math.Max(1, Stage.ActualHeight);
        var time = new Duration(duration);

        // Ease ușor la capete → mișcarea pornește și se oprește lin
        var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
        Animate(layer, scale, ScaleTransform.ScaleXProperty, m.StartScale, m.EndScale, time, ease);
        Animate(layer, scale, ScaleTransform.ScaleYProperty, m.StartScale, m.EndScale, time, ease);
        Animate(layer, translate, TranslateTransform.XProperty, m.StartOffsetX * width, m.EndOffsetX * width, time, ease);
        Animate(layer, translate, TranslateTransform.YProperty, m.StartOffsetY * height, m.EndOffsetY * height, time, ease);
    }

    private void Animate(Image layer, Animatable target, DependencyProperty property, double from, double to,
        Duration duration, IEasingFunction ease)
    {
        var clock = new DoubleAnimation(from, to, duration) { EasingFunction = ease, FillBehavior = FillBehavior.HoldEnd }.CreateClock();
        target.ApplyAnimationClock(property, clock);
        _motionClocks[layer].Add(clock);
    }

    private void StopMotion(Image layer)
    {
        foreach (var clock in _motionClocks[layer]) clock.Controller?.Stop();
        _motionClocks[layer].Clear();
    }

    private void PauseMotion(Image layer)
    {
        foreach (var clock in _motionClocks[layer]) clock.Controller?.Pause();
    }

    private void ResumeMotion(Image layer)
    {
        foreach (var clock in _motionClocks[layer]) clock.Controller?.Resume();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SlideshowViewModel.IsPlaying) || ViewModel is not { } vm) return;
        // Pauză: mișcarea Ken Burns îngheață pe loc (poza rămâne pe ecran); reluare din același punct
        foreach (var layer in new[] { LayerA, LayerB })
            if (vm.IsPlaying) ResumeMotion(layer); else PauseMotion(layer);
        _chrome.Show();
    }

    // ------------------------------------------------------------------ Tastatură

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        var vm = ViewModel;
        switch (e.Key)
        {
            case Key.Escape: Close(); break;
            case Key.Space: vm?.TogglePlayCommand.Execute(null); break;
            case Key.M: vm?.AddMusicCommand.Execute(null); break;
            case Key.L: vm?.ToggleLoopCommand.Execute(null); _chrome.Show(); break;
            case Key.Right or Key.PageDown: vm?.NextCommand.Execute(null); _chrome.Show(); break;
            case Key.Left or Key.PageUp: vm?.PreviousCommand.Execute(null); _chrome.Show(); break;
            default: return;
        }
        e.Handled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopMotion(LayerA);
        StopMotion(LayerB);
        ViewModel?.Dispose();
        base.OnClosed(e);
    }
}
