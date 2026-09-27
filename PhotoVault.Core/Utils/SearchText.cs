using System.Globalization;
using System.Text;

namespace PhotoVault.Core.Utils;

/// <summary>
/// Potrivire pentru căutarea simplă (§6.7): fără diferență de majuscule și diacritice
/// („vacanta" găsește „Vacanță"), iar toți termenii introduși trebuie să apară (AND).
/// Textele se normalizează o singură dată (<see cref="Normalize"/>), apoi comparația e ordinală —
/// rapidă și pe zeci de mii de poze, la fiecare tastă.
/// </summary>
public static class SearchText
{
    /// <summary>Împarte textul căutat în termeni normalizați (spații / virgule).</summary>
    public static string[] Terms(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? []
            : query.Split([' ', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries).Select(Normalize).ToArray();

    /// <summary>Litere mici, fără diacritice (ă → a, ș → s, é → e).</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var ascii = true;
        foreach (var c in text)
        {
            if (c > 0x7F)
            {
                ascii = false;
                break;
            }
        }
        if (ascii) return text.ToLowerInvariant();

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(c));
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>true dacă fiecare termen (din <see cref="Terms"/>) apare în cel puțin unul dintre câmpurile deja normalizate.</summary>
    public static bool MatchesNormalized(IReadOnlyList<string> terms, string? field1, string? field2 = null)
    {
        foreach (var term in terms)
        {
            if ((field1 is null || !field1.Contains(term, StringComparison.Ordinal)) &&
                (field2 is null || !field2.Contains(term, StringComparison.Ordinal)))
                return false;
        }
        return true;
    }

    /// <summary>Variantă comodă: câmpurile sunt normalizate aici (pentru apeluri izolate).</summary>
    public static bool Matches(IReadOnlyList<string> terms, params string?[] fields)
    {
        var normalized = fields.Select(Normalize).ToArray();
        return terms.All(term => normalized.Any(f => f.Contains(term, StringComparison.Ordinal)));
    }
}
