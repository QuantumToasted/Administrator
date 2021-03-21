using System;
using Disqord;
using Disqord.Models;
using Newtonsoft.Json;

namespace Administrator.Common
{
    public sealed class MappedEmoji : IEmoji
    {
        [JsonProperty("primaryName")]
        public string PrimaryName { get; set; }

        [JsonProperty("primaryNameWithColons")]
        public string PrimaryNameWithColons { get; set; }

        [JsonProperty("names")]
        public string[] Names { get; set; }

        [JsonProperty("namesWithColons")]
        public string[] NamesWithColons { get; set; }

        [JsonProperty("surrogates")]
        public string Surrogates { get; set; }

        [JsonProperty("utf32codepoints")]
        public long[] Utf32Codepoints { get; set; }

        [JsonProperty("assetFileName")]
        public string AssetFileName { get; set; }

        [JsonProperty("assetUrl")]
        public Uri AssetUrl { get; set; }

        IClient IEntity.Client => throw new NotImplementedException();
        bool IEquatable<IEmoji>.Equals(IEmoji other) => other switch
        {
            Emoji emoji => emoji.GetMessageFormat().Equals(Surrogates),
            LocalEmoji localEmoji => localEmoji.GetMessageFormat().Equals(Surrogates),
            MappedEmoji mappedEmoji => mappedEmoji.Surrogates.Equals(Surrogates),
            _ => false
        };
        void IJsonUpdatable<EmojiJsonModel>.Update(EmojiJsonModel model) => throw new NotImplementedException();
        string IEmoji.Name => Surrogates;
    }
}