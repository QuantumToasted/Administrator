using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Svg;

namespace Administrator.Bot;

public sealed class EmojiService(HttpClient http, IMemoryCache cache) : DiscordBotService
{
    private const int MAPPED_EMOJI_SIZE = 1000;
    private const string REMOTE_DATA_LOCATION = "https://static.emzi0767.com/misc/discordEmojiMap.json";
    private const string LOCAL_DATA_LOCATION = "Data/emojiMappings.json";

    public static readonly Regex CustomEmojiRegex = new(@"<a?:([a-zA-Z0-9_]+):([0-9]+)>", RegexOptions.Compiled);
    
    private readonly HashSet<ReactionInstance> _previousReactions = new();

    public IReadOnlyDictionary<string, MappedEmoji> Surrogates { get; private set; } = new Dictionary<string, MappedEmoji>();

    public IReadOnlyDictionary<string, MappedEmoji> Names { get; private set; } = new Dictionary<string, MappedEmoji>();

    public async ValueTask<MemoryStream> GetOrCreateDefaultEmojiAsync(MappedEmoji emoji)
    {
        var bytes = await cache.GetOrCreateAsync<byte[]>($"E:{emoji.Surrogates}", async _ =>
        {
            await using var svgStream = await http.GetMemoryStreamAsync(emoji.AssetUrl);
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

            return output.ToArray();
        });

        return new MemoryStream(bytes!);
    }

    public bool TryParseEmoji(string emojiString, [NotNullWhen(true)] out IEmoji? emoji)
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

        emoji = null;
        return false;
    }

    public IEmoji ParseEmoji(string emojiString)
    {
        return TryParseEmoji(emojiString, out var emoji)
            ? emoji
            : throw new FormatException($"The supplied string \"{emojiString}\" was unable to be parsed as a valid emoji.");
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var data = await FetchLocalDataAsync(cancellationToken, true);
        if (data is not null)
        {
            // Is the data older than a month?
            if ((DateTimeOffset.UtcNow - data.VersionTimestamp).TotalDays < 30)
            {
                Logger.LogDebug("Successfully fetched local mapping emoji data that is less than 1 month old. Skipping remote data fetch.");
                PopulateMaps(data);
                return;
            }
        }

        data = await FetchRemoteDataAsync(cancellationToken);
        if (data is not null)
        {
            Logger.LogDebug("Successfully fetched remote emoji mapping data.");
            PopulateMaps(data);
            await WriteDataAsync(data, cancellationToken);
            return;
        }

        Logger.LogWarning("Falling back to local emoji mapping data due to remote emoji data being unavailable.");
        data = await FetchLocalDataAsync(cancellationToken);
        if (data is not null)
        {
            Logger.LogDebug("Successfully fetched backup local emoji mapping data.");
            PopulateMaps(data);
            await WriteDataAsync(data, cancellationToken);
            return;
        }

        Logger.LogError("Local and remote emoji mapping data were unable to be loaded.");
        //await host.StopAsync(cancellationToken);

        void PopulateMaps(EmojiMappingData mappingData)
        {
            var surrogates = new Dictionary<string, MappedEmoji>();
            var names = new Dictionary<string, MappedEmoji>();

            foreach (var emoji in mappingData.EmojiDefinitions)
            {
                surrogates.Add(emoji.Surrogates, emoji);
                
                if (!string.IsNullOrWhiteSpace(emoji.AlternateSurrogates))
                    surrogates.Add(emoji.AlternateSurrogates, emoji);

                foreach (var name in emoji.Names)
                {
                    names.Add(name, emoji);
                }
            }

            Surrogates = surrogates;
            Names = names;

            Logger.LogInformation("Loaded {EmojiCount} unique emojis with {EmojiNameCount} unique names.",
                Surrogates.Count, Names.Count);
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (!e.GuildId.HasValue ||
            e.Message.Author.IsBot ||
            !string.IsNullOrWhiteSpace(e.Message.Content))
        {
            return;
        }

        var trackedEmojiIds = new HashSet<Snowflake>();
        var guildEmojis = Bot.GetGuild(e.GuildId.Value)?.Emojis
                          ?? new Dictionary<Snowflake, IGuildEmoji>();

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        foreach (var match in CustomEmojiRegex.Matches(e.Message.Content).ToList())
        {
            Snowflake emojiId;

            try
            {
                emojiId = Snowflake.Parse(match.Groups[1].Value);
            }
            catch
            {
                continue;
            }

            if (trackedEmojiIds.Contains(emojiId) || !guildEmojis.ContainsKey(emojiId))
                continue;

            var emojiStatistics = await db.GetOrCreateEmojiStatisticsAsync(e.GuildId.Value, emojiId);
            emojiStatistics.Uses++;

            trackedEmojiIds.Add(emojiId);
        }

        await db.SaveChangesAsync();
    }

    protected override async ValueTask OnReactionAdded(ReactionAddedEventArgs e)
    {
        if (e.GuildId is not { } guildId ||
            e.Emoji is not ICustomEmoji emoji ||
            !_previousReactions.Add(new ReactionInstance(e.MessageId, e.UserId)))
        {
            return;
        }

        var guildEmojis = Bot.GetGuild(guildId)?.Emojis
                          ?? new Dictionary<Snowflake, IGuildEmoji>();

        if (!guildEmojis.ContainsKey(emoji.Id))
            return;

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        var emojiStatistics = await db.GetOrCreateEmojiStatisticsAsync(e.GuildId.Value, emoji.Id);
        emojiStatistics.Uses++;

        await db.SaveChangesAsync();
    }

    private async Task<EmojiMappingData?> FetchRemoteDataAsync(CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        string json;

        try
        {
            response = await http.GetAsync(REMOTE_DATA_LOCATION, cancellationToken);
            response.EnsureSuccessStatusCode();

            json = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Remote emoji mapping data was unable to be fetched from {Path}.", REMOTE_DATA_LOCATION);
            return null;
        }
        finally
        {
            response?.Dispose();
        }

        try
        {
            return JsonConvert.DeserializeObject<EmojiMappingData>(json);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Remote emoji mapping data was unable to be mapped.");
        }

        return null;
    }

    private async Task<EmojiMappingData?> FetchLocalDataAsync(CancellationToken cancellationToken, bool firstLoad = false)
    {
        string json;

        try
        {
            Directory.CreateDirectory("Data");
            json = await File.ReadAllTextAsync(LOCAL_DATA_LOCATION, cancellationToken);
        }
        catch (FileNotFoundException) when (firstLoad)
        {
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Local emoji mapping data was unable to be read from {Path}.", LOCAL_DATA_LOCATION);
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<EmojiMappingData>(json);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Local emoji mapping data was unable to be mapped.");
        }

        return null;
    }

    private async Task WriteDataAsync(EmojiMappingData data, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonConvert.SerializeObject(data, Formatting.None);
            await File.WriteAllTextAsync(LOCAL_DATA_LOCATION, json, cancellationToken);
            Logger.LogDebug("Emoji mapping data successfully written to {Path}.", LOCAL_DATA_LOCATION);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Local emoji mapping data was unable to be written to {Path}.", LOCAL_DATA_LOCATION);
        }
    }

    private readonly record struct ReactionInstance(Snowflake MessageId, Snowflake UserId);
}