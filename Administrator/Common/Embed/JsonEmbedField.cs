using Disqord;
using Newtonsoft.Json;

namespace Administrator.Common
{
    public sealed class JsonEmbedField
    {
        public JsonEmbedField(LocalEmbedField field)
        {
            Name = field.Name;
            Value = field.Value;
            IsInline = field.IsInline;
        }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("value")] 
        public string Value { get; private set; }

        [JsonProperty("inline")]
        public bool IsInline { get; private set; }
    }
}