using Disqord;
using Newtonsoft.Json;

namespace Administrator.Common
{
    public sealed class JsonEmbedFooter
    {
        public JsonEmbedFooter(LocalEmbedFooter footer)
        {
            Text = footer.Text;
            IconUrl = footer.IconUrl;
        }

        [JsonProperty("text")]
        public string Text { get; private set; }

        [JsonProperty("icon")]
        public string IconUrl { get; private set; }
    }
}