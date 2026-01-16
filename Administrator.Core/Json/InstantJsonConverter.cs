using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Administrator.Core;

public sealed class InstantJsonConverter : JsonConverter<Instant>
{
    public override Instant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => NodaConverters.InstantConverter.Read(ref reader, typeToConvert, options);

    public override void Write(Utf8JsonWriter writer, Instant value, JsonSerializerOptions options)
        => NodaConverters.InstantConverter.Write(writer, value, options);
}