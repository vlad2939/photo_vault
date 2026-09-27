using Dapper;
using PhotoVault.Core.Abstractions;
using PhotoVault.Core.Models;

namespace PhotoVault.Data.Repositories;

public sealed class TagRepository(DatabaseContext db) : ITagRepository
{
    public IReadOnlyList<TagSummary> GetAll()
    {
        using var connection = db.OpenConnection();
        return connection.Query<(long Id, string Name, long Count)>(
                """
                SELECT t.Id, t.Name, (SELECT COUNT(*) FROM PhotoTags pt WHERE pt.TagId = t.Id)
                FROM Tags t
                """)
            .Select(r => new TagSummary(new Tag { Id = r.Id, Name = r.Name }, (int)r.Count))
            .OrderBy(t => t.Tag.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Comparație fără diferență majuscule/minuscule, inclusiv pentru diacritice („Ș" = „ș").</summary>
    public Tag? FindByName(string name)
    {
        using var connection = db.OpenConnection();
        return connection.Query<(long Id, string Name)>("SELECT Id, Name FROM Tags")
            .Where(t => string.Equals(t.Name, name, StringComparison.CurrentCultureIgnoreCase))
            .Select(t => new Tag { Id = t.Id, Name = t.Name })
            .FirstOrDefault();
    }

    public Tag Create(string name)
    {
        using var connection = db.OpenConnection();
        var id = connection.ExecuteScalar<long>("INSERT INTO Tags (Name) VALUES (@name); SELECT last_insert_rowid();", new { name });
        return new Tag { Id = id, Name = name };
    }

    public void Rename(long tagId, string name)
    {
        using var connection = db.OpenConnection();
        connection.Execute("UPDATE Tags SET Name = @name WHERE Id = @tagId", new { tagId, name });
    }

    public void Delete(long tagId)
    {
        using var connection = db.OpenConnection();
        connection.Execute("DELETE FROM Tags WHERE Id = @tagId", new { tagId });   // cascadă → PhotoTags
    }

    public int Assign(long tagId, IReadOnlyCollection<long> photoIds)
    {
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var added = connection.Execute("INSERT OR IGNORE INTO PhotoTags (PhotoId, TagId) VALUES (@photoId, @tagId)",
            photoIds.Select(photoId => new { photoId, tagId }), transaction);
        transaction.Commit();
        return added;
    }

    public void Unassign(long tagId, long photoId)
    {
        using var connection = db.OpenConnection();
        connection.Execute("DELETE FROM PhotoTags WHERE TagId = @tagId AND PhotoId = @photoId", new { tagId, photoId });
    }

    public IReadOnlyList<Tag> GetForPhoto(long photoId)
    {
        using var connection = db.OpenConnection();
        return connection.Query<(long Id, string Name)>(
                "SELECT t.Id, t.Name FROM Tags t JOIN PhotoTags pt ON pt.TagId = t.Id WHERE pt.PhotoId = @photoId", new { photoId })
            .Select(t => new Tag { Id = t.Id, Name = t.Name })
            .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public IReadOnlySet<long> GetPhotoIds(long tagId)
    {
        using var connection = db.OpenConnection();
        return connection.Query<long>("SELECT PhotoId FROM PhotoTags WHERE TagId = @tagId", new { tagId }).ToHashSet();
    }
}
