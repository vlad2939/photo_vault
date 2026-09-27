using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoVault.App.Utils;

/// <summary>
/// Încărcare asincronă a miniaturilor de pe disc (decodare pe thread pool, BitmapImage
/// înghețat) + cache LRU mic, ca derularea înainte/înapoi să nu re-decodeze constant.
/// </summary>
public static class ThumbnailCache
{
    private const int Capacity = 600;

    private static readonly Dictionary<string, LinkedListNode<(string Path, ImageSource Image)>> Map = new(StringComparer.OrdinalIgnoreCase);
    private static readonly LinkedList<(string Path, ImageSource Image)> Order = new();
    private static readonly SemaphoreSlim Decoders = new(4, 4);

    /// <summary>Apelat doar din thread-ul UI.</summary>
    public static async Task<ImageSource?> GetAsync(string path)
    {
        if (Map.TryGetValue(path, out var node))
        {
            Order.Remove(node);
            Order.AddFirst(node);
            return node.Value.Image;
        }

        await Decoders.WaitAsync();
        ImageSource? image;
        try
        {
            image = await Task.Run(() => Decode(path));
        }
        finally
        {
            Decoders.Release();
        }
        if (image is null) return null;

        if (!Map.ContainsKey(path))
        {
            Map[path] = Order.AddFirst((path, image));
            if (Order.Count > Capacity)
            {
                Map.Remove(Order.Last!.Value.Path);
                Order.RemoveLast();
            }
        }
        return image;
    }

    /// <summary>Scoate o intrare din cache (ex. miniatură regenerată).</summary>
    public static void Invalidate(string path)
    {
        if (!Map.Remove(path, out var node)) return;
        Order.Remove(node);
    }

    private static ImageSource? Decode(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;   // fișierul nu rămâne blocat
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            bitmap.Freeze();                                  // utilizabil din thread-ul UI
            return bitmap;
        }
        catch (Exception e) when (e is IOException or NotSupportedException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}
