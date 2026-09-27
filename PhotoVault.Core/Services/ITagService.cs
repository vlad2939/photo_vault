using PhotoVault.Core.Models;

namespace PhotoVault.Core.Services;

/// <summary>Tag-uri custom (§6.6).</summary>
public interface ITagService
{
    IReadOnlyList<TagSummary> GetTags();

    /// <summary>Creează tag-ul sau îl întoarce pe cel existent cu același nume (fără diferență majuscule/minuscule).</summary>
    Tag GetOrCreate(string name);

    /// <summary>false dacă există deja alt tag cu numele nou.</summary>
    bool TryRename(long tagId, string name);

    void Delete(long tagId);
    int Assign(long tagId, IReadOnlyCollection<long> photoIds);
    void Unassign(long tagId, long photoId);
    IReadOnlyList<Tag> GetTagsForPhoto(long photoId);
    IReadOnlySet<long> GetPhotoIds(long tagId);
    IReadOnlySet<long> GetTaggedPhotoIds();
}
