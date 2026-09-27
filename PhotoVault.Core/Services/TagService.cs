using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

public sealed class TagService(ITagRepository tags) : ITagService
{
    public IReadOnlyList<TagSummary> GetTags() => tags.GetAll();

    public Tag GetOrCreate(string name)
    {
        var normalized = NormalizeName(name);
        return tags.FindByName(normalized) ?? tags.Create(normalized);
    }

    public bool TryRename(long tagId, string name)
    {
        var normalized = NormalizeName(name);
        var existing = tags.FindByName(normalized);
        if (existing is not null && existing.Id != tagId) return false;
        tags.Rename(tagId, normalized);
        return true;
    }

    public void Delete(long tagId) => tags.Delete(tagId);

    public int Assign(long tagId, IReadOnlyCollection<long> photoIds) =>
        photoIds.Count == 0 ? 0 : tags.Assign(tagId, photoIds);

    public void Unassign(long tagId, long photoId) => tags.Unassign(tagId, photoId);

    public IReadOnlyList<Tag> GetTagsForPhoto(long photoId) => tags.GetForPhoto(photoId);

    public IReadOnlySet<long> GetPhotoIds(long tagId) => tags.GetPhotoIds(tagId);

    public IReadOnlySet<long> GetTaggedPhotoIds() => tags.GetTaggedPhotoIds();

    /// <summary>Spațiile multiple sunt comprimate; tag-ul nu poate fi gol.</summary>
    private static string NormalizeName(string name)
    {
        var collapsed = string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length == 0) throw new ArgumentException("Numele tag-ului nu poate fi gol.", nameof(name));
        return collapsed;
    }
}
