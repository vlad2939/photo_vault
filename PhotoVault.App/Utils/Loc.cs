using System.Globalization;
using System.Windows;

namespace PhotoVault.App.Utils;

/// <summary>
/// Localizare UI (§6.12): textele stau în Resources/Localization/Strings.{ro|en}.xaml.
/// Limba se încarcă o singură dată, la pornire (schimbarea cere repornire).
/// </summary>
public static class Loc
{
    private const string LocalizationFolder = "/Localization/";

    /// <summary>Cultura folosită la formatarea numerelor/datelor în UI (ex. „1.240” în română).</summary>
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("ro-RO");

    public static void Apply(string language)
    {
        var code = language == "en" ? "en" : "ro";
        Culture = CultureInfo.GetCultureInfo(code == "en" ? "en-US" : "ro-RO");
        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/PhotoVault;component/Resources/Localization/Strings.{code}.xaml", UriKind.Absolute)
        };

        var merged = Application.Current.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d => d.Source?.OriginalString.Contains(LocalizationFolder, StringComparison.OrdinalIgnoreCase) == true);
        if (existing is null)
            merged.Add(dictionary);
        else
            merged[merged.IndexOf(existing)] = dictionary;
    }

    /// <summary>Textul localizat pentru cheia dată (sau cheia, dacă lipsește).</summary>
    public static string Get(string key) =>
        Application.Current?.TryFindResource(key) as string ?? key;

    /// <summary>Text localizat cu parametri (șablonul folosește {0}, {1}...).</summary>
    public static string Format(string key, params object?[] args) =>
        string.Format(Culture, Get(key), args);

    /// <summary>
    /// „N poze" cu acordul corect: RO — 1 poză, 2–19 poze, 20+ „de poze" (inclusiv 101–119 → poze);
    /// EN — 1 photo / N photos.
    /// </summary>
    public static string PhotoCount(long count) => Plural("Str.Count", count);

    /// <summary>„N fișiere" cu același acord (1 fișier, 2–19 fișiere, 20+ „de fișiere").</summary>
    public static string FileCount(long count) => Plural("Str.FileCount", count);

    private static string Plural(string prefix, long count)
    {
        var suffix = count == 1 ? ".One"
            : Culture.TwoLetterISOLanguageName == "ro" && count != 0 && (count % 100 is 0 or >= 20) ? ".Many"
            : ".Few";
        return Format(prefix + suffix, Number(count));
    }

    /// <summary>Număr formatat cu separatorul de mii al limbii curente.</summary>
    public static string Number(long value) => value.ToString("N0", Culture);
}
