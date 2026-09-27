using PhotoVault.Core.Models;
using PhotoVault.Core.Services;
using PhotoVault.Core.Utils;
using SixLabors.ImageSharp;
using Xunit.Abstractions;

namespace PhotoVault.Tests.Utils;

/// <summary>
/// Validarea extragerii previzualizării pe fișiere RAW reale (CR2 / NEF / DNG, §9 Faza 8).
/// Fișierele nu sunt în repo (dimensiune, licențe): CI-ul le descarcă din surse publice în folderul dat de
/// variabila de mediu RAW_SAMPLES_DIR și rulează doar acest test (Category=RealRaw). Local, fără variabilă, testul nu face nimic.
/// </summary>
[Trait("Category", "RealRaw")]
public sealed class RealRawSamplesTests(ITestOutputHelper output)
{
    [Fact]
    public void RealRawFiles_YieldDisplayablePreviewAndThumbnail()
    {
        var dir = Environment.GetEnvironmentVariable("RAW_SAMPLES_DIR");
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            output.WriteLine("RAW_SAMPLES_DIR nu e setat — test sărit.");
            return;
        }

        var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Where(f => SupportedFormats.NormalizeExtension(f) is { } ext && SupportedFormats.IsRaw(ext))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count == 0)
        {
            // Sursele publice pot fi indisponibile temporar — descărcarea e „cel mai bun efort"
            output.WriteLine($"Nicio mostră RAW în {dir} — test sărit.");
            return;
        }

        var thumbnails = new ThumbnailService(Path.Combine(dir, "_thumbs"), new MetadataService());
        var failures = new List<string>();
        var id = 0;
        foreach (var file in files)
        {
            var name = Path.GetFileName(file);
            var preview = RawPreviewExtractor.ExtractLargestJpeg(file);
            if (preview is null)
            {
                failures.Add($"{name}: nicio previzualizare JPEG afișabilă");
                output.WriteLine($"✗ {name}: nicio previzualizare JPEG afișabilă");
                continue;
            }

            var info = Image.Identify(preview);
            var thumb = thumbnails.Generate(new PhotoItem
            {
                Id = ++id,
                FullPath = file,
                FileName = name,
                Extension = SupportedFormats.NormalizeExtension(file)!,
            });
            var ok = thumb.ThumbnailPath is not null;
            if (!ok) failures.Add($"{name}: miniatura nu a putut fi generată");
            output.WriteLine($"{(ok ? "✓" : "✗")} {name}: previzualizare {info.Width}×{info.Height} ({preview.Length / 1024:N0} KB)" +
                             $", miniatură {(ok ? "OK" : "eșuată")}, data EXIF {thumb.DateTaken?.ToString("yyyy-MM-dd") ?? "—"}");
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
