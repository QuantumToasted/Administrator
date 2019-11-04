using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
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

        [JsonIgnore]
        public CultureInfo Culture
        {
            get
            {
                try
                {
                    return new CultureInfo(CultureCode);
                }
                catch (CultureNotFoundException)
                {
                    return null;
                }
            }
        }

        // Most reasonable way to manually add and update responses
        public void UpdateResponses(IDictionary<string, ImmutableArray<string>> newResponses)
        {
            Responses = (IReadOnlyDictionary<string, ImmutableArray<string>>) newResponses;
        }
    }
}