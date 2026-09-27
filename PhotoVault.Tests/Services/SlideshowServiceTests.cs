using PhotoVault.Core.Models;
using PhotoVault.Core.Services;

namespace PhotoVault.Tests.Services;

public class SlideshowServiceTests
{
    [Fact]
    public void NextMotion_ZoomInAndOut_AreInverted()
    {
        var service = new SlideshowService(new Random(1));
        var motions = Enumerable.Range(0, 200).Select(_ => service.NextMotion(1.2, 10)).ToList();

        var zoomIn = motions.First(m => m.ZoomIn);
        var zoomOut = motions.First(m => !m.ZoomIn);
        Assert.Equal(1.1, zoomIn.StartScale, 6);
        Assert.Equal(1.1 * 1.2, zoomIn.EndScale, 6);
        Assert.Equal(zoomIn.StartScale, zoomOut.EndScale, 6);
        Assert.Equal(zoomIn.EndScale, zoomOut.StartScale, 6);
    }

    [Fact]
    public void NextMotion_UsesAllDirections_IndependentlyOfZoom()
    {
        var service = new SlideshowService(new Random(7));
        var motions = Enumerable.Range(0, 400).Select(_ => service.NextMotion(1.15, 15)).ToList();

        // Toate cele 8 combinații zoom × direcție apar
        Assert.Equal(8, motions.Select(m => (m.ZoomIn, m.Pan)).Distinct().Count());
    }

    [Theory]
    [InlineData(1.05, 5)]
    [InlineData(1.30, 25)]
    [InlineData(1.15, 10)]
    public void NextMotion_NeverRevealsImageEdges(double zoom, double pan)
    {
        var service = new SlideshowService(new Random(3));
        foreach (var m in Enumerable.Range(0, 100).Select(_ => service.NextMotion(zoom, pan)))
        {
            // Marginea rămâne în afara cadrului cât timp |deplasare| ≤ (scară − 1) / 2
            Assert.True(Math.Abs(m.StartOffsetX) <= (m.StartScale - 1) / 2 + 1e-9);
            Assert.True(Math.Abs(m.StartOffsetY) <= (m.StartScale - 1) / 2 + 1e-9);
            Assert.True(Math.Abs(m.EndOffsetX) <= (m.EndScale - 1) / 2 + 1e-9);
            Assert.True(Math.Abs(m.EndOffsetY) <= (m.EndScale - 1) / 2 + 1e-9);
            Assert.Equal(-m.StartOffsetX, m.EndOffsetX);   // traversează diagonala
            Assert.Equal(-m.StartOffsetY, m.EndOffsetY);
        }
    }

    [Fact]
    public void Playlist_LoopsAndSkipsMissingFiles()
    {
        var service = new SlideshowService();
        var existing = Path.GetTempFileName();
        try
        {
            var tracks = service.GetPlayableTracks([existing, Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp3")]);
            Assert.Equal([existing], tracks);
        }
        finally
        {
            File.Delete(existing);
        }

        Assert.Equal(1, service.NextTrack(0, 3));
        Assert.Equal(0, service.NextTrack(2, 3));   // buclă
        Assert.Equal(0, service.NextTrack(0, 1));
        Assert.Equal(-1, service.NextTrack(0, 0));
    }
}
