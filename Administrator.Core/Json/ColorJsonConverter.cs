using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Disqord;

namespace Administrator.Core;

public sealed class ColorJsonConverter : JsonConverter<Color?>
{
    private static readonly Dictionary<string, Color> PresetColors = GenerateColors();
    
    public static readonly ColorJsonConverter Instance = new();
    
    public override bool HandleNull => true;

    public override Color? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        
        // try parsing as a raw int
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var rawColorValue))
            return rawColorValue;
        
        // try parsing as a hex code
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        if (value[0] == '#')
            value = value[1..];

        if (value.Length == 6 && int.TryParse(value, NumberStyles.HexNumber, null, out var hexColor))
            return hexColor;

        // try parsing from a color name
        if (PresetColors.TryGetValue(value, out var namedColor))
            return namedColor;

        throw new FormatException("Invalid color format. Must be a raw integer value, hex code #ABCDEF, or a color name.");
    }

    public override void Write(Utf8JsonWriter writer, Color? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());

    private static Dictionary<string, Color> GenerateColors()
    {
        var properties = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static);
        var colors = new Dictionary<string, Color>(properties.Length - 1, StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties)
        {
            if (property.Name == nameof(Color.Random))
                continue;

            colors[property.Name] = (Color) property.GetValue(null)!;
        }

        return colors;
    }
}