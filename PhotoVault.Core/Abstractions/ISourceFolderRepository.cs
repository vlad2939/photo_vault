using PhotoVault.Core.Models;

namespace PhotoVault.Core.Abstractions;

public interface ISourceFolderRepository
{
    IReadOnlyList<SourceFolder> GetAll();
    SourceFolder? GetById(long id);
    SourceFolder Add(string folderPath);
    void Delete(long id);
    void UpdateLastScanned(long id, DateTime lastScanned);

    /// <summary>
    /// Mută folderul sursă la o cale nouă (realiniere, §11): actualizează calea folderului și, în aceeași tranzacție,
    /// prefixul căii tuturor pozelor lui. Id-urile rămân → albumele, tag-urile și rotirile se păstrează.
    /// </summary>
    void Relocate(long id, string newFolderPath);
}
