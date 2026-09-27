using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PhotoVault.Tests;

/// <summary>Imagini de test generate pe loc (JPEG, PNG și un RAW sintetic bazat pe TIFF).</summary>
internal static class TestImages
{
    public static void WriteJpeg(string path, int width = 800, int height = 600)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = new Image<Rgb24>(width, height, new Rgb24(200, 120, 40));
        image.SaveAsJpeg(path);
    }

    public static void WritePng(string path, int width = 400, int height = 400)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = new Image<Rgb24>(width, height, new Rgb24(40, 120, 200));
        image.SaveAsPng(path);
    }

    public static byte[] JpegBytes(int width, int height)
    {
        using var image = new Image<Rgb24>(width, height, new Rgb24(10, 200, 90));
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Fișier TIFF little-endian care imită un CR2: IFD0 cu previzualizarea JPEG mare (Compression = 6),
    /// IFD1 cu un thumbnail mic (JPEGInterchangeFormat) și IFD2 cu „datele RAW" — un JPEG lossless (SOF3),
    /// mai mare decât previzualizarea, pe care extractorul trebuie să-l ignore.
    /// </summary>
    public static void WriteSyntheticRaw(string path, byte[] largePreview, byte[] smallThumb, ushort orientation = 1)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var rawData = LosslessJpegStub(largePreview.Length * 3);

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        // Header: "II", 42, offset IFD0 = 8
        w.Write((byte)'I'); w.Write((byte)'I'); w.Write((ushort)42); w.Write(8u);

        const int ifd0Entries = 5, ifd1Entries = 2, ifd2Entries = 3;
        var ifd1Offset = 8 + IfdSize(ifd0Entries);
        var ifd2Offset = ifd1Offset + IfdSize(ifd1Entries);
        var largeOffset = ifd2Offset + IfdSize(ifd2Entries);
        var smallOffset = largeOffset + largePreview.Length;
        var rawOffset = smallOffset + smallThumb.Length;

        w.Write((ushort)ifd0Entries);
        Entry(w, 0x0103, 3, 1, 6);                              // Compression = JPEG (vechi)
        Entry(w, 0x0111, 4, 1, (uint)largeOffset);              // StripOffsets
        Entry(w, 0x0112, 3, 1, orientation);                    // Orientation
        Entry(w, 0x0117, 4, 1, (uint)largePreview.Length);      // StripByteCounts
        Entry(w, 0x0118, 3, 1, 3);                              // SamplesPerPixel (valoare oarecare)
        w.Write((uint)ifd1Offset);

        w.Write((ushort)ifd1Entries);
        Entry(w, 0x0201, 4, 1, (uint)smallOffset);              // JPEGInterchangeFormat
        Entry(w, 0x0202, 4, 1, (uint)smallThumb.Length);        // JPEGInterchangeFormatLength
        w.Write((uint)ifd2Offset);

        w.Write((ushort)ifd2Entries);
        Entry(w, 0x0103, 3, 1, 6);                              // „JPEG" — dar lossless (datele RAW)
        Entry(w, 0x0111, 4, 1, (uint)rawOffset);
        Entry(w, 0x0117, 4, 1, (uint)rawData.Length);
        w.Write(0u);

        w.Write(largePreview);
        w.Write(smallThumb);
        w.Write(rawData);
        File.WriteAllBytes(path, ms.ToArray());
    }

    /// <summary>Început de JPEG lossless (SOI + SOF3), umplut până la lungimea cerută.</summary>
    public static byte[] LosslessJpegStub(int length)
    {
        var data = new byte[length];
        byte[] header = [0xFF, 0xD8, 0xFF, 0xC3, 0x00, 0x0B, 0x0C, 0x0F, 0xA0, 0x13, 0x60, 0x01, 0x01, 0x11, 0x00];
        header.CopyTo(data, 0);
        return data;
    }

    private static int IfdSize(int entries) => 2 + entries * 12 + 4;

    private static void Entry(BinaryWriter w, ushort tag, ushort type, uint count, uint value)
    {
        w.Write(tag); w.Write(type); w.Write(count);
        if (type == 3) { w.Write((ushort)value); w.Write((ushort)0); }
        else w.Write(value);
    }
}
