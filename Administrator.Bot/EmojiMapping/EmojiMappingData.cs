using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Administrator.Bot;

public sealed record EmojiMappingData(
    [property: JsonPropertyName("version")]
        string Version,
    [property: JsonPropertyName("versionTimestamp")]
        DateTimeOffset VersionTimestamp,
    [property: JsonPropertyName("emojiDefinitions")]
        ImmutableArray<MappedEmoji> EmojiDefinitions);