using System.Globalization;

namespace PhotoVault.Data;

/// <summary>Datele sunt stocate ca text ISO 8601 (§4), fără fus orar (ora locală).</summary>
internal static class SqliteDates
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss";

    public static string ToDb(DateTime value) => value.ToString(Format, CultureInfo.InvariantCulture);

    public static string? ToDb(DateTime? value) => value is null ? null : ToDb(value.Value);

    public static DateTime FromDb(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);

    public static DateTime? FromDbNullable(string? value) =>
        string.IsNullOrEmpty(value) ? null : FromDb(value);
}
