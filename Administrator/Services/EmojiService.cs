using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Common;
using Disqord;
using Disqord.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Svg;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Administrator.Services
{
    public sealed class EmojiService : DiscordClientService
    {
        private const int MAPPED_EMOJI_SIZE = 1000;
        private const string EMOJI_MAP_URL = "https://static.emzi0767.com/misc/discordEmojiMap.json";
        private const string DATA_LOCATION = "Data/emojiMappings.json";

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;

        public EmojiService(ILogger<EmojiService> logger, AdministratorBot bot)
            : base(logger, bot)
        {
            _http = bot.Services.GetRequiredService<HttpClient>();
            _cache = bot.Services.GetRequiredService<IMemoryCache>();
        }
        
        public IReadOnlyDictionary<string, MappedEmoji> Surrogates { get; private set; }

        public IReadOnlyDictionary<string, MappedEmoji> Names { get; private set; }

        public async ValueTask<MemoryStream> GetOrCreateDefaultEmojiAsync(MappedEmoji emoji)
        {
            if (_cache.TryGetValue(emoji.Surrogates, out byte[] bytes))
                return new MemoryStream(bytes);

            await using var svgStream = await _http.GetStreamAsync(emoji.AssetUrl);
            var doc = SvgDocument.Open<SvgDocument>(svgStream);
            
            var ratio = Math.Max(doc.Width / doc.Height, doc.Height / doc.Width);
            doc.Height = doc.Height > doc.Width
                ? MAPPED_EMOJI_SIZE
                : MAPPED_EMOJI_SIZE * ratio;

            doc.Width = doc.Height > doc.Width
                ? MAPPED_EMOJI_SIZE * ratio
                : MAPPED_EMOJI_SIZE;

            await using var output = new MemoryStream();
            using var bitmap = doc.Draw();
            bitmap.Save(output, ImageFormat.Png);
            output.Seek(0, SeekOrigin.Begin);

            return new MemoryStream(_cache.Set(emoji.Surrogates, output.ToArray(),
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10))));
        }

        public bool TryParseEmoji(string emojiString, out IEmoji emoji)
        {
            if (LocalCustomEmoji.TryParse(emojiString, out var localCustomEmoji))
            {
                emoji = localCustomEmoji;
                return true;
            }

            if (Surrogates.TryGetValue(emojiString, out var mappedEmoji))
            {
                emoji = mappedEmoji;
                return true;
            }

            if (Names.TryGetValue(emojiString.ToLower(), out mappedEmoji))
            {
                emoji = mappedEmoji;
                return true;
            }

            emoji = default;
            return false;
        }

        public IEmoji ParseEmoji(string emojiString)
        {
            return TryParseEmoji(emojiString, out var emoji)
                ? emoji
                : throw new FormatException("The supplied string was unable to be parsed as a valid emoji.");
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using var response = await _http.GetAsync(EMOJI_MAP_URL, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("Failed to retrieve remote emoji map, falling back to local emoji map.");

                try
                {
                    var json = await File.ReadAllTextAsync(DATA_LOCATION, cancellationToken);
                    var data = JsonConvert.DeserializeObject<EmojiMappingData>(json);
                    PopulateMaps(data);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Local emoji map data was unable to be mapped! Emoji parsing will no longer work properly.");
                }

                return;
            }

            try
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonConvert.DeserializeObject<EmojiMappingData>(json);
                PopulateMaps(data);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Remote emoji map data was unable to be mapped! Emoji parsing will no longer work properly.");
            }
        }

        private void PopulateMaps(EmojiMappingData data)
        {
            var surrogates = new Dictionary<string, MappedEmoji>();
            var names = new Dictionary<string, MappedEmoji>();

            foreach (var emoji in data.EmojiDefinitions)
            {
                surrogates.Add(emoji.Surrogates, emoji);
                
                foreach (var name in emoji.Names)
                {
                    names.Add(name, emoji);
                }
            }

            Surrogates = surrogates;
            Names = names;
        }
    }
}