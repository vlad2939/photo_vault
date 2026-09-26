using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace PhotoVault.Core.Services;

public sealed class MetadataService : IMetadataService
{
    public PhotoMetadata Read(string path)
    {
        IReadOnlyList<MetadataExtractor.Directory> directories;
        try
        {
            directories = ImageMetadataReader.ReadMetadata(path);
        }
        catch (Exception e) when (e is ImageProcessingException or IOException or UnauthorizedAccessException)
        {
            return new PhotoMetadata(null, 1);
        }

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
}
