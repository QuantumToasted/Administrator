using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json;

namespace Administrator.Common
{
    public sealed class LocalizedLanguage
    {
        [JsonProperty("code")]
        public string CultureCode { get; private set; }

        [JsonProperty("englishName")]
        public string EnglishName { get; private set; }

        [JsonProperty("nativeName")]
        public string NativeName { get; private set; }

        [JsonProperty("responses")]
        public IReadOnlyDictionary<string, ImmutableArray<string>> Responses { get; private set; }
    }
}