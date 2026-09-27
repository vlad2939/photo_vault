using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = MetadataExtractor.Directory;

namespace PhotoVault.Core.Utils;

/// <summary>
/// Extrage cea mai mare previzualizare JPEG încorporată într-un fișier RAW
/// (CR2, NEF, DNG — toate bazate pe TIFF), fără decodarea datelor RAW (§6.3).
/// Locațiile candidate sunt citite prin MetadataExtractor din toate IFD-urile:
///  • JPEGInterchangeFormat / Length (0x0201 / 0x0202) — NEF JpgFromRaw, thumbnail IFD1
///  • StripOffsets / StripByteCounts (0x0111 / 0x0117) cu compresie JPEG — CR2 IFD0, DNG SubIFD
/// Fiecare candidat e validat: trebuie să fie un JPEG baseline/progressive (SOF0–SOF2).
/// Datele RAW propriu-zise din CR2/DNG sunt tot „JPEG", dar lossless (SOF3) — nedecodabile
/// de ImageSharp/WPF — și sunt de obicei cel mai mare bloc din fișier, deci trebuie excluse.
/// </summary>
public static class RawPreviewExtractor
{
    private const int TagCompression = 0x0103;
    private const int TagStripOffsets = 0x0111;
    private const int TagStripByteCounts = 0x0117;
    private const int TagJpegOffset = 0x0201;
    private const int TagJpegLength = 0x0202;

    /// <summary>Octeții celui mai mare JPEG încorporat, sau null dacă nu există.</summary>
    public static byte[]? ExtractLargestJpeg(string path)
    {
        using var stream = File.OpenRead(path);
        IReadOnlyList<Directory> directories;
        try
        {
            directories = ImageMetadataReader.ReadMetadata(stream);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return null;
        }

        var candidates = new List<(long Offset, long Length)>();
        foreach (var dir in directories.OfType<ExifDirectoryBase>())
        {
            if (TryGetLong(dir, TagJpegOffset, out var offset) && TryGetLong(dir, TagJpegLength, out var length))
                candidates.Add((offset, length));

            // Strip unic comprimat JPEG (6 = JPEG vechi, 7 = JPEG) — previzualizarea mare din CR2/DNG
            if (TryGetLong(dir, TagCompression, out var compression) && compression is 6 or 7 &&
                TryGetLong(dir, TagStripOffsets, out var stripOffset) && TryGetLong(dir, TagStripByteCounts, out var stripLength))
                candidates.Add((stripOffset, stripLength));
        }

        foreach (var (offset, length) in candidates.Distinct().OrderByDescending(c => c.Length))
        {
            if (offset <= 0 || length < 4 || offset + length > stream.Length) continue;

            // Verificarea tipului se face pe primii octeți, înainte de a citi tot blocul (datele RAW au zeci de MB)
            var head = new byte[(int)Math.Min(length, 64 * 1024)];
            stream.Position = offset;
            stream.ReadExactly(head);
            if (!IsDisplayableJpeg(head)) continue;

            var buffer = new byte[length];
            stream.Position = offset;
            stream.ReadExactly(buffer);
            return buffer;
        }
        return null;
    }

    /// <summary>
    /// true dacă datele încep cu un JPEG pe care decodoarele uzuale îl suportă:
    /// primul marker SOF este SOF0 (baseline), SOF1 (extended) sau SOF2 (progressive).
    /// </summary>
    public static bool IsDisplayableJpeg(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8) return false;

        var pos = 2;
        while (pos + 4 <= data.Length)
        {
            if (data[pos] != 0xFF) return false;
            var marker = data[pos + 1];
            if (marker == 0xFF) { pos++; continue; }                   // octeți de umplere
            if (marker is 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7)) { pos += 2; continue; }   // markeri fără lungime

            // SOF0..SOF15 (fără DHT=C4, JPG=C8, DAC=CC)
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
                return marker is 0xC0 or 0xC1 or 0xC2;
            if (marker is 0xDA or 0xD9) return false;                   // începutul datelor fără SOF

            var segmentLength = (data[pos + 2] << 8) | data[pos + 3];
            if (segmentLength < 2) return false;
            pos += 2 + segmentLength;
        }
        return false;
    }

    /// <summary>Valoarea numerică a unui tag; pentru tablouri (mai multe strip-uri) — doar dacă e unul singur.</summary>
    private static bool TryGetLong(Directory dir, int tag, out long value)
    {
        value = 0;
        switch (dir.GetObject(tag))
        {
            case null:
                return false;
            case Array { Length: 1 } single:
                return TryConvert(single.GetValue(0), out value);
            case Array:
                return false;
            case var scalar:
                return TryConvert(scalar, out value);
        }
    }

    private static bool TryConvert(object? raw, out long value)
    {
        try
        {
            value = Convert.ToInt64(raw, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
        {
            value = 0;
            return false;
        }
    }
}
