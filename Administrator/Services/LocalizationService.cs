using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Newtonsoft.Json;

namespace Administrator.Services
{
    public sealed class LocalizationService : IService
    {
        private readonly LoggingService _logging;
        private readonly Random _random;

        public LocalizationService(LoggingService logging, Random random)
        {
            _logging = logging;
            _random = random;
            Languages = new List<LocalizedLanguage>();
        }

        public ICollection<LocalizedLanguage> Languages { get; }

        public async Task ReloadAsync()
        {
            Languages.Clear();
            
            try
            {
                foreach (var file in Directory.GetFiles("./Data/Responses/")
                    .Where(x => x.EndsWith("json", true, CultureInfo.CurrentCulture)))
                {
                    Languages.Add(JsonConvert.DeserializeObject<LocalizedLanguage>(await File.ReadAllTextAsync(file)));
                }
            }
            catch (Exception ex)
            {
                await _logging.LogCriticalAsync($"Failed to load all localization files.\n{ex}", "Localization");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        public string Localize(LocalizedLanguage language, string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            if (language.Responses.TryGetValue(key, out var responses))
            {
                var response = responses[_random.Next(0, responses.Length)];

                try
                {
                    return string.Format(new CultureInfo(language.CultureCode), response, args);
                }
                catch
                {
                    return string.Format(response, args);
                }
            }
            
            _logging.LogErrorAsync(
                $"Response key {key} is missing from {language.CultureCode} localization file.",
                "Localization");

            if (language.CultureCode.Equals("en-US"))
            {
                return
                    $"Response key `{key}` was not present in one or more localization files.\n" +
                    "Please report this to the bot owner.";
            }

            return Localize(Languages.First(x => x.CultureCode.Equals("en-US")), key, args);
        }

        Task IService.InitializeAsync()
            => ReloadAsync();
    }
}