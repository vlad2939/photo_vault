using System.IO;
using System.Windows;
using NAudio.Wave;
using PhotoVault.Core.Services;

namespace PhotoVault.App.Utils;

/// <summary>Redarea playlist-ului din slideshow (abstracție pentru ViewModel).</summary>
public interface IMusicPlayer : IDisposable
{
    /// <summary>Pornește redarea în buclă; false dacă nu există nicio piesă redabilă sau niciun dispozitiv audio.</summary>
    /// <param name="startWith">Piesa cu care începe redarea (ex. piesele tocmai adăugate din slideshow); implicit prima.</param>
    bool Start(IReadOnlyList<string> playlist, int volumePercent, string? startWith = null);

    /// <summary>Numele piesei care începe să fie redată (pe thread-ul UI).</summary>
    event Action<string>? TrackStarted;

    void Pause();
    void Resume();
}

/// <summary>
/// Playlist MP3 în buclă cu NAudio (§6.10): <c>AudioFileReader</c> + <c>WaveOut</c> (fostul <c>WaveOutEvent</c>), trecere manuală la piesa
/// următoare la finalul fiecăreia, reluare de la prima după ultima. Piesele ilizibile sunt sărite.
/// </summary>
public sealed class MusicPlayer(ISlideshowService slideshow) : IMusicPlayer
{
    private IReadOnlyList<string> _tracks = [];
    private int _index = -1;
    private float _volume = 0.7f;
    private WaveOut? _output;
    private AudioFileReader? _reader;
    private bool _paused;
    private bool _disposed;

    public event Action<string>? TrackStarted;

    public bool Start(IReadOnlyList<string> playlist, int volumePercent, string? startWith = null)
    {
        if (_disposed) return false;
        ReleaseTrack();
        _paused = false;
        _tracks = slideshow.GetPlayableTracks(playlist);
        _volume = Math.Clamp(volumePercent, 0, 100) / 100f;
        if (_tracks.Count == 0) return false;
        var start = startWith is null ? -1 : _tracks.ToList().FindIndex(t => string.Equals(t, startWith, StringComparison.OrdinalIgnoreCase));
        _index = start >= 0 ? start - 1 : -1;   // PlayNext avansează la indexul următor
        return PlayNext();
    }

    public void Pause()
    {
        _paused = true;
        _output?.Pause();
    }

    public void Resume()
    {
        _paused = false;
        _output?.Play();
    }

    /// <summary>Trece la următoarea piesă redabilă; după o tură completă fără succes, renunță.</summary>
    private bool PlayNext()
    {
        for (var attempt = 0; attempt < _tracks.Count && !_disposed; attempt++)
        {
            _index = slideshow.NextTrack(_index, _tracks.Count);
            ReleaseTrack();
            try
            {
                _reader = new AudioFileReader(_tracks[_index]) { Volume = _volume };
                _output = new WaveOut();
                _output.PlaybackStopped += OnPlaybackStopped;
                _output.Init(_reader);
                if (!_paused) _output.Play();
                TrackStarted?.Invoke(Path.GetFileNameWithoutExtension(_tracks[_index]));
                return true;
            }
            catch (NAudio.MmException)
            {
                // Niciun dispozitiv de ieșire audio (ex. calculator fără placă de sunet) → slideshow fără muzică
                ReleaseTrack();
                return false;
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                // Fișier șters între timp / MP3 ilizibil (NAudio aruncă tipuri variate, ex. InvalidDataException) → piesa următoare
                ReleaseTrack();
            }
        }
        return false;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // Totul se petrece pe thread-ul UI, indiferent de unde vine evenimentul
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => OnPlaybackStopped(sender, e));
            return;
        }
        if (_disposed || sender != _output) return;
        if (e.Exception is not null)
        {
            ReleaseTrack();
            return;
        }
        PlayNext();
    }

    private void ReleaseTrack()
    {
        if (_output is not null)
        {
            _output.PlaybackStopped -= OnPlaybackStopped;
            _output.Dispose();
            _output = null;
        }
        _reader?.Dispose();
        _reader = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ReleaseTrack();
    }
}
