using System.Globalization;
using System.Text;

namespace PhotoVault.Core.Utils;

/// <summary>
/// Pattern de redenumire batch (§6.8). Variabile:
/// <c>{name}</c> numele original fără extensie, <c>{counter}</c> / <c>{counter:000}</c> numărător cu padding,
/// <c>{date}</c> / <c>{date:yyyyMMdd}</c> data fișierului de pe disc, <c>{ext}</c> extensia originală.
/// Extensia se păstrează automat la finalul numelui.
/// </summary>
public sealed class RenamePattern
{
    public const string DefaultDateFormat = "yyyy-MM-dd";

    private readonly IReadOnlyList<Func<RenameTokenValues, string>> _parts;

    private RenamePattern(IReadOnlyList<Func<RenameTokenValues, string>> parts) => _parts = parts;

    /// <summary>Analizează pattern-ul; la eroare întoarce null și <paramref name="error"/> (cod de eroare, vezi <see cref="RenamePatternError"/>).</summary>
    public static RenamePattern? TryParse(string? pattern, out RenamePatternError error, out string? detail)
    {
        error = RenamePatternError.None;
        detail = null;
        pattern = (pattern ?? string.Empty).Trim();

        // „.{ext}” la final e redundant: extensia se adaugă oricum
        if (pattern.EndsWith(".{ext}", StringComparison.OrdinalIgnoreCase)) pattern = pattern[..^6];

        if (pattern.Length == 0)
        {
            error = RenamePatternError.Empty;
            return null;
        }

        var parts = new List<Func<RenameTokenValues, string>>();
        var literal = new StringBuilder();
        var i = 0;
        while (i < pattern.Length)
        {
            var c = pattern[i];
            if (c == '}')
            {
                error = RenamePatternError.UnbalancedBrace;
                return null;
            }
            if (c != '{')
            {
                literal.Append(c);
                i++;
                continue;
            }

            var end = pattern.IndexOf('}', i + 1);
            var nextOpen = pattern.IndexOf('{', i + 1);
            if (end < 0 || (nextOpen >= 0 && nextOpen < end))
            {
                error = RenamePatternError.UnbalancedBrace;
                return null;
            }

            if (literal.Length > 0)
            {
                var text = literal.ToString();
                parts.Add(_ => text);
                literal.Clear();
            }

            var token = pattern[(i + 1)..end];
            var colon = token.IndexOf(':');
            var name = (colon < 0 ? token : token[..colon]).Trim().ToLowerInvariant();
            var format = colon < 0 ? null : token[(colon + 1)..];

            Func<RenameTokenValues, string>? part = name switch
            {
                "name" when format is null => v => v.Name,
                "ext" when format is null => v => v.Extension,
                "counter" => CounterPart(format),
                "date" => DatePart(format),
                _ => null,
            };
            if (part is null)
            {
                error = RenamePatternError.UnknownToken;
                detail = "{" + token + "}";
                return null;
            }
            parts.Add(part);
            i = end + 1;
        }

        if (literal.Length > 0)
        {
            var text = literal.ToString();
            parts.Add(_ => text);
        }
        return new RenamePattern(parts);
    }

    /// <summary>Numele rezultat (fără extensie).</summary>
    public string FormatStem(string originalName, int counter, DateTime fileDate)
    {
        var values = new RenameTokenValues(
            Path.GetFileNameWithoutExtension(originalName),
            Path.GetExtension(originalName).TrimStart('.'),
            counter,
            fileDate);
        var builder = new StringBuilder();
        foreach (var part in _parts) builder.Append(part(values));
        return builder.ToString();
    }

    private static Func<RenameTokenValues, string>? CounterPart(string? format)
    {
        if (string.IsNullOrEmpty(format)) return v => v.Counter.ToString(CultureInfo.InvariantCulture);
        // Doar zerouri: {counter:000} → 001, 002, ...
        if (format.Any(ch => ch != '0') || format.Length > 9) return null;
        var width = format.Length;
        return v => v.Counter.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
    }

    private static Func<RenameTokenValues, string>? DatePart(string? format)
    {
        format = string.IsNullOrWhiteSpace(format) ? DefaultDateFormat : format;
        try
        {
            _ = new DateTime(2024, 1, 31, 13, 45, 10).ToString(format, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return null;
        }
        return v => v.FileDate.ToString(format, CultureInfo.InvariantCulture);
    }

    private readonly record struct RenameTokenValues(string Name, string Extension, int Counter, DateTime FileDate);
}

public enum RenamePatternError
{
    None,
    Empty,
    UnbalancedBrace,
    UnknownToken
}
