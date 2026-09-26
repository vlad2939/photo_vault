using System.Windows;

namespace PhotoVault.App.Utils;

/// <summary>
/// Localizare UI (§6.12): textele stau în Resources/Localization/Strings.{ro|en}.xaml.
/// Limba se încarcă o singură dată, la pornire (schimbarea cere repornire).
/// </summary>
public static class Loc
{
    private const string LocalizationFolder = "/Localization/";

    public static void Apply(string language)
    {
        var code = language == "en" ? "en" : "ro";
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
}
