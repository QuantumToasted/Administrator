using System;
using Disqord;
using Disqord.Models;
using Newtonsoft.Json;

namespace Administrator.Common
{
    public sealed class MappedEmoji : IEmoji
    {
        [JsonProperty("primaryName")]
        public string PrimaryName { get; private set; }

        [JsonProperty("primaryNameWithColons")]
        public string PrimaryNameWithColons { get; private set; }

        [JsonProperty("names")]
        public string[] Names { get; private set; }

        [JsonProperty("namesWithColons")]
        public string[] NamesWithColons { get; private set; }

        [JsonProperty("surrogates")]
        public string Surrogates { get; private set; }

        [JsonProperty("utf32codepoints")]
        public long[] Utf32Codepoints { get; private set; }

        [JsonProperty("assetFileName")]
        public string AssetFileName { get; private set; }

        [JsonProperty("assetUrl")]
        public Uri AssetUrl { get; private set; }

        public override bool Equals(object obj)
            => ((IEquatable<IEmoji>) this).Equals(obj as IEmoji);

        public override int GetHashCode()
            => Discord.Comparers.Emoji.GetHashCode(this);

        bool IEquatable<IEmoji>.Equals(IEmoji other) => other switch
        {
            Emoji emoji => emoji.GetMessageFormat().Equals(Surrogates),
            LocalEmoji localEmoji => localEmoji.GetMessageFormat().Equals(Surrogates),
            MappedEmoji mappedEmoji => mappedEmoji.Surrogates.Equals(Surrogates),
            _ => false
        };
        IClient IEntity.Client => throw new NotImplementedException();
        void IJsonUpdatable<EmojiJsonModel>.Update(EmojiJsonModel model) => throw new NotImplementedException();
        string IEmoji.Name => Surrogates;
    }
}