using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

public sealed class SlideshowService(Random? random = null) : ISlideshowService
{
    private readonly Random _random = random ?? Random.Shared;

    public KenBurnsMotion NextMotion(double zoomIntensity, double panPercent)
    {
        var zoom = Math.Clamp(zoomIntensity, 1.0, 2.0);
        var pan = Math.Clamp(panPercent, 0, 50) / 100.0;

        // Scara de bază acoperă deplasarea: la ±pan/2 marginile imaginii nu intră niciodată în cadru
        var baseScale = 1 + pan;
        var zoomIn = _random.Next(2) == 0;
        var (startScale, endScale) = zoomIn ? (baseScale, baseScale * zoom) : (baseScale * zoom, baseScale);

        var direction = (PanDirection)_random.Next(4);
        var half = pan / 2;
        var (sx, sy) = direction switch
        {
            // Imaginea deplasată spre dreapta-jos arată colțul stânga-sus; apoi alunecă spre colțul opus
            PanDirection.TopLeftToBottomRight => (half, half),
            PanDirection.TopRightToBottomLeft => (-half, half),
            PanDirection.BottomLeftToTopRight => (half, -half),
            _ => (-half, -half),
        };
        return new KenBurnsMotion(zoomIn, direction, startScale, endScale, sx, sy, -sx, -sy);
    }

    public IReadOnlyList<string> GetPlayableTracks(IReadOnlyList<string> playlist) =>
        playlist.Where(File.Exists).ToList();

    public int NextTrack(int current, int count) => count <= 0 ? -1 : (current + 1) % count;
}
