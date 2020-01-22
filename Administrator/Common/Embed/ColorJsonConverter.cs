using System;
using System.Globalization;
using Disqord;
using Newtonsoft.Json;

namespace Administrator.Common
{
    public sealed class ColorJsonConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteValue(value.RawValue);
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            return reader.Value switch
            {
                ulong uint64 when uint64 < int.MaxValue => new Color(Convert.ToInt32(uint64)),
                long int64 when int64 < int.MaxValue => new Color(Convert.ToInt32(int64)),
                uint uint32 when uint32 < int.MaxValue => new Color(Convert.ToInt32(uint32)),
                int int32 => new Color(int32),
                string str when int.TryParse(str.TrimStart('#'), 
                    NumberStyles.HexNumber, default, out var value) => new Color(value),
                _ => throw new FormatException("The color must either be a hex code or a valid integer color value.")
            };
        }
    }
}