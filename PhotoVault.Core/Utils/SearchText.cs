using System.Globalization;

namespace PhotoVault.Core.Utils;

/// <summary>
/// Potrivire pentru căutarea simplă (§6.7): fără diferență de majuscule și diacritice
/// („vacanta" găsește „Vacanță"), iar toți termenii introduși trebuie să apară (AND).
/// </summary>
public static class SearchText
{
    private static readonly CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions Options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>Împarte textul căutat în termeni (spații / virgule).</summary>
    public static string[] Terms(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? []
            : query.Split([' ', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries);

    /// <summary>true dacă fiecare termen apare în cel puțin unul dintre câmpuri.</summary>
    public static bool Matches(IReadOnlyList<string> terms, params string?[] fields)
    {
        foreach (var term in terms)
        {
            var found = false;
            foreach (var field in fields)
            {
                if (field is not null && Compare.IndexOf(field, term, Options) >= 0)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }
}
