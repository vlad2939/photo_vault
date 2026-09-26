using PhotoVault.Core.Models;

namespace PhotoVault.Core.Abstractions;

public interface ISourceFolderRepository
{
    IReadOnlyList<SourceFolder> GetAll();
    SourceFolder? GetById(long id);
    SourceFolder Add(string folderPath);
    void Delete(long id);
    void UpdateLastScanned(long id, DateTime lastScanned);
}
