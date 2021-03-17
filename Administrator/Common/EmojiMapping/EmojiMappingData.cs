using System;
using System.Collections.Immutable;
using Newtonsoft.Json;

namespace Administrator.Common
{
    public sealed class EmojiMappingData
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("versionTimestamp")]
        public DateTimeOffset VersionTimestamp { get; set; }

        [JsonProperty("emojiDefinitions")]
        public ImmutableArray<MappedEmoji> EmojiDefinitions { get; set; }
    }
}