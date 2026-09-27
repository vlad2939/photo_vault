using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;
using PhotoVault.Core.Utils;

namespace PhotoVault.App.Utils;

/// <summary>
/// Încarcă imaginea la rezoluție mare pentru lightbox (§6.4), pe thread pool:
///  • JPEG/PNG — fișierul original;
///  • RAW — cea mai mare previzualizare JPEG încorporată (fără decodare RAW).
/// Orientarea EXIF și rotirea logică (RotationDegrees) sunt aplicate la afișare; fișierul rămâne neatins.
/// </summary>
public static class FullImageLoader
{
    /// <summary>Latura maximă decodată — suficient pentru zoom pe ecrane 4K, memorie rezonabilă.</summary>
    private const int MaxDecodeSize = 8000;

    public static Task<BitmapSource?> LoadAsync(PhotoItem photo, IMetadataService metadata) =>
        Task.Run(() => Load(photo, metadata));

    private static BitmapSource? Load(PhotoItem photo, IMetadataService metadata)
    {
        try
        {
            var orientation = metadata.Read(photo.FullPath).Orientation;

            // Fișierul e citit integral în memorie → nu rămâne blocat pe disc cât timp e afișat
            var bytes = SupportedFormats.IsRaw(photo.Extension)
                ? RawPreviewExtractor.ExtractLargestJpeg(photo.FullPath)
                : File.ReadAllBytes(photo.FullPath);
            if (bytes is null) return null;

            BitmapSource bitmap = Decode(bytes);
            var angle = (OrientationToAngle(orientation) + photo.RotationDegrees) % 360;
            if (angle != 0)
            {
                bitmap = new TransformedBitmap(bitmap, new RotateTransform(angle));
                bitmap.Freeze();
            }
            return bitmap;
        }
        catch (Exception e)
        {
            // Orice imagine ilizibilă → mesaj în lightbox; detaliile tehnice ajung în logs/
            ErrorLog.Write(e, $"Lightbox: încărcare {photo.FullPath}");
            return null;
        }
    }

    private static BitmapSource Decode(byte[] bytes)
    {
        bool landscape;
        using (var stream = new MemoryStream(bytes, writable: false))
        {
            var frame = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            if (Math.Max(frame.PixelWidth, frame.PixelHeight) <= MaxDecodeSize)
            {
                frame.Freeze();
                return frame;
            }
            landscape = frame.PixelWidth >= frame.PixelHeight;
        }

        // Imagine foarte mare: re-decodare direct la dimensiune redusă (memorie rezonabilă)
        using var large = new MemoryStream(bytes, writable: false);
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = large;
        image.CacheOption = BitmapCacheOption.OnLoad;
        if (landscape) image.DecodePixelWidth = MaxDecodeSize; else image.DecodePixelHeight = MaxDecodeSize;
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <summary>Orientare EXIF → unghi de rotire (variantele „oglindite" 2/4/5/7 sunt aproximate prin rotire).</summary>
    private static int OrientationToAngle(int orientation) => orientation switch
    {
        3 or 4 => 180,
        5 or 6 => 90,
        7 or 8 => 270,
        _ => 0,
    };
}
