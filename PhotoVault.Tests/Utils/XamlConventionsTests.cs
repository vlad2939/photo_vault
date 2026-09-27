using System.Text.RegularExpressions;

namespace PhotoVault.Tests.Utils;

/// <summary>
/// Verificări statice pe fișierele XAML ale aplicației, pentru greșeli care compilează dar opresc o fereastră
/// abia la deschidere (WPF nu poate rula pe mașinile de build Linux, deci ele ar fi prinse doar de smoke test-ul Windows).
/// </summary>
public class XamlConventionsTests
{
    private static string AppDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PhotoVault.sln"))) dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("PhotoVault.sln"), "PhotoVault.App");
    }

    private static IEnumerable<string> XamlFiles() =>
        Directory.EnumerateFiles(AppDirectory(), "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    [Fact]
    public void BasedOn_NeverUsesDynamicResource()
    {
        // „A 'DynamicResourceExtension' cannot be set on the 'BasedOn' property" — XamlParseException la runtime
        var offenders = XamlFiles()
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, line, i)))
            .Where(x => x.line.Contains("BasedOn=\"{DynamicResource", StringComparison.Ordinal))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}")
            .ToList();
        Assert.True(offenders.Count == 0, "BasedOn cu DynamicResource: " + string.Join(", ", offenders));
    }

    [Fact]
    public void ResourceKeys_AreUniquePerDictionary()
    {
        // O cheie duplicată în același dicționar oprește aplicația la pornire
        var duplicates = new List<string>();
        foreach (var file in XamlFiles())
        {
            var keys = Regex.Matches(File.ReadAllText(file), "x:Key=\"([^\"]+)\"").Select(m => m.Groups[1].Value);
            duplicates.AddRange(keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => $"{Path.GetFileName(file)}: {g.Key}"));
        }
        Assert.True(duplicates.Count == 0, "Chei duplicate: " + string.Join(", ", duplicates));
    }

    [Fact]
    public void ThemeDictionaries_DefineTheSameKeys()
    {
        // Tema Light trebuie să aibă aceleași chei ca Dark (altfel elementele rămân necolorate după comutare)
        static HashSet<string> Keys(string path) =>
            Regex.Matches(File.ReadAllText(path), "x:Key=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToHashSet();
        var themes = Path.Combine(AppDirectory(), "Themes");
        var dark = Keys(Path.Combine(themes, "DarkTheme.xaml"));
        var light = Keys(Path.Combine(themes, "LightTheme.xaml"));
        Assert.Empty(dark.Except(light));
        Assert.Empty(light.Except(dark));
    }
}
