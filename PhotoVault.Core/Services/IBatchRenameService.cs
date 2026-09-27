using PhotoVault.Core.Models;
using PhotoVault.Core.Utils;

namespace PhotoVault.Core.Services;

/// <summary>Redenumire batch pe disc, cu previzualizare obligatorie și verificare de coliziuni (§6.8).</summary>
public interface IBatchRenameService
{
    /// <summary>Fișierele foto suportate direct din folder (fără subfoldere).</summary>
    IReadOnlyList<RenameSource> GetFiles(string folderPath);

    /// <summary>Numele rezultate + starea fiecărui rând (fără nicio modificare pe disc).</summary>
    IReadOnlyList<RenamePlanItem> BuildPlan(string folderPath, IReadOnlyList<RenameSource> files, RenamePattern pattern,
        int counterStart, RenameOrder order);

    /// <summary>Câte poze din folder sunt deja în index (0 = folder neindexat).</summary>
    int CountIndexed(string folderPath);

    /// <summary>
    /// Redenumește fizic rândurile <see cref="RenameStatus.Ok"/>; refuză (<see cref="BatchRenameException"/>) dacă planul are conflicte
    /// sau discul s-a schimbat între timp. Pozele deja indexate își păstrează albumele, tag-urile și rotirea.
    /// </summary>
    RenameResult Apply(string folderPath, IReadOnlyList<RenamePlanItem> plan);
}
