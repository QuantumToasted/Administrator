using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Disqord;

namespace Administrator.Core;

public sealed class ColorJsonConverter : JsonConverter<Color?>
{
    public static readonly ColorJsonConverter Instance = new();
    
    public override bool HandleNull => true;

    public override Color? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        
        // try parsing as a raw int
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var rawIntValue))
            return rawIntValue;
        
        // try parsing as a hex code
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        if (value[0] == '#')
            value = value[1..];

        if (value.Length > 6 || !int.TryParse(value, NumberStyles.HexNumber, null, out var rawValue))
            throw new FormatException("Invalid color string provided. Must be a 6-character hex color code.");

        return rawValue;
    }

    public override void Write(Utf8JsonWriter writer, Color? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}