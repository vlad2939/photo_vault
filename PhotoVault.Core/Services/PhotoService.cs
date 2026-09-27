using PhotoVault.Core.Abstractions;

namespace PhotoVault.Core.Services;

public sealed class PhotoService(IPhotoRepository photos) : IPhotoService
{
    public IReadOnlyDictionary<long, int> RotateClockwise(IReadOnlyCollection<long> photoIds)
    {
        if (photoIds.Count == 0) return new Dictionary<long, int>();
        var current = photos.GetRotations(photoIds);
        // Fișierul original nu e atins — doar valoarea logică din DB (§6.9)
        var updated = current.ToDictionary(kv => kv.Key, kv => (kv.Value + 90) % 360);
        photos.SetRotations(updated);
        return updated;
    }

    public IReadOnlyDictionary<long, string> GetSearchKeywords() => photos.GetSearchKeywords();
}
