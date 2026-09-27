using PhotoVault.Core.Models;

namespace PhotoVault.Core.Abstractions;

public interface ITagRepository
{
    IReadOnlyList<TagSummary> GetAll();
    Tag? FindByName(string name);
    Tag Create(string name);
    void Rename(long tagId, string name);

    /// <summary>Elimină tag-ul de pe toate pozele și îl șterge (§6.6).</summary>
    void Delete(long tagId);

    int Assign(long tagId, IReadOnlyCollection<long> photoIds);
    void Unassign(long tagId, long photoId);
    IReadOnlyList<Tag> GetForPhoto(long photoId);
    IReadOnlySet<long> GetPhotoIds(long tagId);
}
