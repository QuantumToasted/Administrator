using System.Text.Json;
using System.Text.Json.Serialization;
using Disqord;

namespace Administrator.Core;

public sealed class SnowflakeJsonConverter : JsonConverter<Snowflake>
{
    public override bool HandleNull => false;

    public override Snowflake Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetUInt64();

    public override void Write(Utf8JsonWriter writer, Snowflake value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}