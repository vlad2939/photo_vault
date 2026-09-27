using PhotoVault.Core.Models;
using PhotoVault.Core.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

namespace PhotoVault.Core.Services;

/// <summary>
/// Miniaturi JPEG 300×300 (max) generate cu ImageSharp:
///  • JPEG/PNG — decodare directă la scară redusă (DecoderOptions.TargetSize, rapid pentru JPEG mari);
///  • RAW — din previzualizarea JPEG încorporată (fără decodare RAW).
/// Orientarea EXIF e aplicată, deci miniatura apare „cu susul în sus".
/// </summary>
public sealed class ThumbnailService(string thumbnailsDirectory, IMetadataService metadata) : IThumbnailService
{
    private static readonly JpegEncoder Encoder = new() { Quality = 85 };

    public int ThumbnailSize => 300;

    public ThumbnailResult Generate(PhotoItem photo)
    {
        var meta = metadata.Read(photo.FullPath);
        var relative = FileHashHelper.ThumbnailRelativePath(photo.FullPath);
        var absolute = GetAbsolutePath(relative);

        try
        {
            if (!File.Exists(absolute))
            {
                using var image = Load(photo, meta.Orientation);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(ThumbnailSize, ThumbnailSize),
                    Mode = ResizeMode.Max,
                }));
                image.Metadata.ExifProfile = null;   // miniatura e deja orientată corect
                image.Metadata.XmpProfile = null;

                Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
                // Scriere atomică: un fișier incomplet (ex. aplicație închisă brusc) nu rămâne în cache
                var temp = absolute + ".tmp";
                image.SaveAsJpeg(temp, Encoder);
                File.Move(temp, absolute, overwrite: true);
            }
            return new ThumbnailResult(photo.Id, relative, meta.DateTaken);
        }
        catch (Exception e) when (e is not OperationCanceledException and not OutOfMemoryException)
        {
            // Orice fișier problematic (format neașteptat, RAW exotic, fișier blocat) primește doar
            // iconița de rezervă — nu trebuie să oprească generarea miniaturilor pentru restul colecției.
            return new ThumbnailResult(photo.Id, null, meta.DateTaken);
        }
    }

    public string GetAbsolutePath(string relativePath) =>
        Path.Combine(thumbnailsDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        try
        {
            File.Delete(GetAbsolutePath(relativePath));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Un fișier de cache rămas orfan nu afectează funcționarea.
        }
    }

    private Image Load(PhotoItem photo, int rawOrientation)
    {
        var options = new DecoderOptions { TargetSize = new Size(ThumbnailSize, ThumbnailSize) };

        if (!SupportedFormats.IsRaw(photo.Extension))
        {
            var image = Image.Load(options, photo.FullPath);
            image.Mutate(x => x.AutoOrient());
            return image;
        }

        var preview = RawPreviewExtractor.ExtractLargestJpeg(photo.FullPath)
                      ?? throw new NotSupportedException("RAW fără previzualizare JPEG încorporată.");
        var rawImage = Image.Load(options, preview);

        // Previzualizarea încorporată nu e rotită; orientarea corectă e cea din IFD0 al fișierului RAW.
        rawImage.Metadata.ExifProfile ??= new ExifProfile();
        rawImage.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)rawOrientation);
        rawImage.Mutate(x => x.AutoOrient());
        return rawImage;
    }
}
