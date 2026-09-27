using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoVault.App.Utils;
using PhotoVault.Core.Models;

namespace PhotoVault.App.ViewModels;

/// <summary>
/// Zona din stânga a footer-ului (§5.5, §5.7): progresul indexării și mesaje scurte de stare.
/// Scanarea are prioritate la afișare față de generarea miniaturilor (care rulează mai mult, în fundal).
/// </summary>
public partial class StatusBarViewModel : ObservableObject
{
    private readonly DispatcherTimer _hideTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private int _activeScans;
    private bool _thumbnailsRunning;
    private int _lastThumbnailCount;

    public StatusBarViewModel()
    {
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            if (_activeScans == 0 && !_thumbnailsRunning) StatusText = null;
        };
    }

    [ObservableProperty]
    public partial string? StatusText { get; set; }

    [ObservableProperty]
    public partial bool IsProgressVisible { get; set; }

    [ObservableProperty]
    public partial bool IsIndeterminate { get; set; }

    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    [ObservableProperty]
    public partial double ProgressMaximum { get; set; } = 1;

    public void BeginScan() => _activeScans++;

    public void EndScan() => _activeScans = Math.Max(0, _activeScans - 1);

    /// <summary>Actualizează footer-ul cu progresul primit de la serviciul de indexare (pe thread-ul UI).</summary>
    public void Report(IndexProgress progress)
    {
        if (progress.Phase == IndexPhase.Thumbnails)
        {
            // Raportările vin din mai multe thread-uri și pot sosi ușor în altă ordine:
            // o valoare mai mică decât ultima văzută e ignorată (Current = 0 marchează o sesiune nouă).
            if (progress.Current != 0 && progress.Current <= _lastThumbnailCount) return;
            _lastThumbnailCount = progress.Current;
            _thumbnailsRunning = progress.Current < progress.Total;
            if (_activeScans > 0) return;   // scanarea curentă rămâne vizibilă
        }

        _hideTimer.Stop();
        switch (progress.Phase)
        {
            case IndexPhase.Scanning:
                ShowProgress(Loc.Get("Str.Status.Scanning"), indeterminate: true);
                break;
            case IndexPhase.Indexing:
                ShowProgress(Loc.Format("Str.Status.Indexing", Loc.Number(progress.Current), Loc.Number(progress.Total)),
                    value: progress.Current, maximum: progress.Total);
                break;
            case IndexPhase.Thumbnails when progress.Current < progress.Total:
                ShowProgress(Loc.Format("Str.Status.Thumbnails", Loc.Number(progress.Current), Loc.Number(progress.Total)),
                    value: progress.Current, maximum: progress.Total);
                break;
            case IndexPhase.Thumbnails:
                ShowMessage(Loc.Format("Str.Status.ThumbnailsDone", Loc.Number(progress.Total)));
                break;
        }
    }

    /// <summary>Mesaj scurt, ascuns automat după câteva secunde.</summary>
    public void ShowMessage(string message)
    {
        IsProgressVisible = false;
        IsIndeterminate = false;
        StatusText = message;
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void ShowProgress(string text, double value = 0, double maximum = 1, bool indeterminate = false)
    {
        StatusText = text;
        IsIndeterminate = indeterminate;
        ProgressMaximum = Math.Max(1, maximum);
        ProgressValue = value;
        IsProgressVisible = true;
    }
}
