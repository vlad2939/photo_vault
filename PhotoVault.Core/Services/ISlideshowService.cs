using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

/// <summary>Logica slideshow-ului (§6.10), independentă de UI: mișcarea Ken Burns și ordinea pieselor din playlist.</summary>
public interface ISlideshowService
{
    /// <summary>
    /// Mișcare aleatorie pentru o poză: zoom in sau zoom out (valori inversate) și una din 4 direcții de pan,
    /// alese independent. <paramref name="zoomIntensity"/> = factorul maxim (1,05–1,30), <paramref name="panPercent"/> = 5–25.
    /// </summary>
    KenBurnsMotion NextMotion(double zoomIntensity, double panPercent);

    /// <summary>Piesele din playlist care încă există pe disc, în ordinea salvată.</summary>
    IReadOnlyList<string> GetPlayableTracks(IReadOnlyList<string> playlist);

    /// <summary>Indexul piesei următoare, cu reluare de la început (buclă).</summary>
    int NextTrack(int current, int count);
}
