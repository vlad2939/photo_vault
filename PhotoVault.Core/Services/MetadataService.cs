using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

public sealed class MetadataService : IMetadataService
{
    public PhotoMetadata Read(string path) => Read(ReadDirectories(path));

    public PhotoDetails ReadDetails(string path)
    {
        var directories = ReadDirectories(path);
        var meta = Read(directories);
        var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        var sub = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault(d => d.ContainsTag(ExifDirectoryBase.TagExposureTime))
                  ?? directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

        var (width, height) = ReadDimensions(directories);
        // Orientările 5–8 înseamnă rotire cu 90° → lățimea și înălțimea afișate se inversează
        if (meta.Orientation >= 5 && width is not null) (width, height) = (height, width);

        var make = ifd0?.GetDescription(ExifDirectoryBase.TagMake)?.Trim();
        var model = ifd0?.GetDescription(ExifDirectoryBase.TagModel)?.Trim();
        // Multe camere repetă producătorul în model („Canon" + „Canon EOS 6D")
        var camera = model is null ? make
            : make is not null && !model.StartsWith(make, StringComparison.OrdinalIgnoreCase) ? $"{make} {model}"
            : model;

        return new PhotoDetails(
            width, height, meta.DateTaken,
            string.IsNullOrWhiteSpace(camera) ? null : camera,
            Clean(sub?.GetDescription(ExifDirectoryBase.TagLensModel)),
            Clean(sub?.GetDescription(ExifDirectoryBase.TagIsoEquivalent)),
            Clean(sub?.GetDescription(ExifDirectoryBase.TagExposureTime)),
            Clean(sub?.GetDescription(ExifDirectoryBase.TagFNumber)),
            Clean(sub?.GetDescription(ExifDirectoryBase.TagFocalLength)));
    }

    private static PhotoMetadata Read(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        if (directories.Count == 0) return new PhotoMetadata(null, 1);

        DateTime? taken = null;
        foreach (var dir in directories.OfType<ExifSubIfdDirectory>())
        {
            if (dir.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var original)) { taken = original; break; }
        }

        var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        if (taken is null && ifd0 is not null && ifd0.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var modified))
            taken = modified;

        var orientation = ifd0 is not null && ifd0.TryGetInt32(ExifDirectoryBase.TagOrientation, out var o) && o is >= 1 and <= 8 ? o : 1;
        return new PhotoMetadata(taken, orientation);
    }

    private static IReadOnlyList<MetadataExtractor.Directory> ReadDirectories(string path)
    {
        try
        {
            return ImageMetadataReader.ReadMetadata(path);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // Metadatele sunt opționale: un fișier cu structură neobișnuită nu trebuie să blocheze nimic
            return [];
        }
    }

    private static (int? Width, int? Height) ReadDimensions(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        foreach (var jpeg in directories.OfType<JpegDirectory>())
            if (jpeg.TryGetInt32(JpegDirectory.TagImageWidth, out var w) && jpeg.TryGetInt32(JpegDirectory.TagImageHeight, out var h))
                return (w, h);
        foreach (var png in directories.OfType<PngDirectory>())
            if (png.TryGetInt32(PngDirectory.TagImageWidth, out var w) && png.TryGetInt32(PngDirectory.TagImageHeight, out var h))
                return (w, h);
        // RAW: dimensiunile senzorului din EXIF (cele mai mari valori găsite)
        int? bestW = null, bestH = null;
        foreach (var dir in directories.OfType<ExifDirectoryBase>())
        {
            if ((dir.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, out var w) && dir.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, out var h)) ||
                (dir.TryGetInt32(ExifDirectoryBase.TagImageWidth, out w) && dir.TryGetInt32(ExifDirectoryBase.TagImageHeight, out h)))
            {
                if (w > (bestW ?? 0)) (bestW, bestH) = (w, h);
            }
        }
        return (bestW, bestH);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
