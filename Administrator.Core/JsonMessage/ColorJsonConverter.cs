using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Disqord;

namespace Administrator.Core;

public class ColorJsonConverter : JsonConverter<Color>
{
    /*
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = reader.Value?.ToString();

        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Missing or empty color string provided.");

        if (value[0] == '#')
            value = value[1..];

        if (value.Length > 6 || !int.TryParse(value, NumberStyles.HexNumber, null, out var rawValue))
            throw new FormatException("Invalid color string provided. Must be a 6-character hex color code.");

        return rawValue;
    }
    */

    public override bool HandleNull => false;

    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Missing or empty color string provided.");

        if (value[0] == '#')
            value = value[1..];

        if (value.Length > 6 || !int.TryParse(value, NumberStyles.HexNumber, null, out var rawValue))
            throw new FormatException("Invalid color string provided. Must be a 6-character hex color code.");

        return rawValue;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}