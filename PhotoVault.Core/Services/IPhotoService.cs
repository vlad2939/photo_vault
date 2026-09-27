namespace PhotoVault.Core.Services;

/// <summary>Operații pe poze deja indexate: rotire logică (§6.9) și cuvinte-cheie pentru căutare (§6.7).</summary>
public interface IPhotoService
{
    /// <summary>Rotește logic pozele cu 90° în sensul acelor de ceasornic; întoarce noile unghiuri (0/90/180/270).</summary>
    IReadOnlyDictionary<long, int> RotateClockwise(IReadOnlyCollection<long> photoIds);

    /// <summary>Pentru fiecare poză cu tag-uri sau albume: textul lor (nume tag-uri + nume albume), folosit la căutare.</summary>
    IReadOnlyDictionary<long, string> GetSearchKeywords();
}
