using Disqord;
using Newtonsoft.Json;

namespace Administrator.Common
{
    public sealed class JsonEmbedAuthor
    {
        public JsonEmbedAuthor(LocalEmbedAuthor author)
        {
            Name = author.Name;
            Url = author.Url;
            IconUrl = author.IconUrl;
        }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("url")]
        public string Url { get; private set;}

        [JsonProperty("icon")]
        public string IconUrl { get; private set; }
    }
}