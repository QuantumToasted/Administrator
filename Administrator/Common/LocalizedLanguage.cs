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

        public string Localize(string str, params object[] args)
        {
            try
            {
                return string.Format(new CultureInfo(CultureCode), str, args);
            }
            catch (CultureNotFoundException)
            {
                return string.Format(str, args);
            }
        }
    }
}