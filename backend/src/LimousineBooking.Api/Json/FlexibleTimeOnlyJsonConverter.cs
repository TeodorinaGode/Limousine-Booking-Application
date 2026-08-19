using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LimousineBooking.Api.Json;

/// <summary>
/// .NET's default TimeOnly converter only accepts the strict "HH:mm:ss" form,
/// but the spec's own examples use "HH:mm", and HTML &lt;input type="time"&gt;
/// elements produce "HH:mm" natively. This accepts either on the way in and
/// always writes "HH:mm:ss" on the way out, for a consistent response shape.
/// </summary>
public class FlexibleTimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value) || !TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var time))
            throw new JsonException($"Invalid time value: '{value}'. Expected a format such as \"08:00\" or \"08:00:00\".");

        return time;
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
}
