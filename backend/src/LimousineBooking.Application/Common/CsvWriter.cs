using System.Globalization;
using System.Text;

namespace LimousineBooking.Application.Common;

/// <summary>Minimal RFC 4180-ish CSV writer — no external dependency needed for the handful of report exports this app produces.</summary>
public static class CsvWriter
{
    public static string Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', headers.Select(Escape)));

        foreach (var row in rows)
            builder.AppendLine(string.Join(',', row.Select(Escape)));

        return builder.ToString();
    }

    public static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Format(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Format(TimeOnly value) => value.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string Escape(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
