using System.Collections.Concurrent;
using Administrator.Core;
using Disqord;
using Disqord.Bot.Hosting;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class AttachmentService(HttpClient http) : DiscordBotService
{
    private readonly ConcurrentDictionary<string, CachedAttachment> _attachments = new();

    public CachedAttachment? GetFromCache(IAttachment attachment)
        => GetFromCache(new Uri(attachment.Url).GetLeftPart(UriPartial.Path));
    
    public CachedAttachment? GetFromCache(string url)
        => _attachments.GetValueOrDefault(url);

    public ValueTask<CachedAttachment> GetAttachment(IAttachment attachment, bool keep = true)
        => GetAttachment(attachment.Url, ignoreQueryOnLookup: true, keep: keep);
    
    public async ValueTask<CachedAttachment> GetAttachment(string url, bool ignoreQueryOnLookup = false, bool keep = true)
    {
        if (ignoreQueryOnLookup)
        {
            if (_attachments.TryGetValue(new Uri(url).GetLeftPart(UriPartial.Path), out var attachment))
                return attachment;
        }
        else if (_attachments.TryGetValue(url, out var attachment))
        {
            return attachment;
        }
        
        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsByteArrayAsync();
        var uri = new Uri(url);
        var filename = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
        var extension = Path.GetExtension(uri.AbsolutePath).ToLower();

        if (string.IsNullOrEmpty(filename) && string.IsNullOrWhiteSpace(extension))
            throw new FormatException($"The url {url} was unable to be mapped to a valid filename and extension.");

        var newAttachment = new CachedAttachment(data, $"{filename}{extension}");

        if (keep)
            _attachments[ignoreQueryOnLookup ? new Uri(url).GetLeftPart(UriPartial.Path) : url] = newAttachment;

        return newAttachment;
    }

    public int ClearOldAttachments()
    {
        var expiryCutoff = DateTimeOffset.UtcNow;
        var attachments = _attachments.Where(x => x.Value.ExpiresAt < expiryCutoff).ToList();
        foreach (var (key, _) in attachments)
        {
            _attachments.TryRemove(key, out _);
        }

        return attachments.Count;
    }

    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.Message is not IUserMessage { Author.IsBot: false, Attachments: { Count: > 0 } attachments } || !e.GuildId.HasValue)
            return;
        
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.LoggingChannels.FindAsync(e.GuildId.Value, LogEventType.MessageDelete) is null)
            return;

        var cachedAttachments = new List<ulong>();
        var now = DateTimeOffset.UtcNow;
        foreach (var attachment in attachments)
        {
            try
            {
                var att = await GetAttachment(attachment);
                cachedAttachments.Add(attachment.Id);
                var dur = att.ExpiresAt - now;
                Logger.LogDebug("Size: {Size}, cache duration: {Duration}", att.Size, dur);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to cache message attachment {Id}.", attachment.Id.RawValue);
            }
        }

        if (cachedAttachments.Count > 0)
            Logger.LogDebug("Cached {Count} new message attachments: {Ids}", cachedAttachments.Count, cachedAttachments);
    }
    
    public record CachedAttachment(byte[] Data, string FileName)
    {
        public DateTimeOffset ExpiresAt { get; } = CalculateExpiry(Data.Length);
        public ByteSize Size { get; } = ByteSize.FromBytes(Data.Length);
        public LocalAttachment ToLocalAttachment() => LocalAttachment.Bytes(Data, FileName);
        public override string ToString() => $"attachment://{FileName}";

        // attachments will be cached for 30 minutes until ~10MB, then drop until 5 minutes at 50MB
        private static DateTimeOffset CalculateExpiry(long sizeInBytes)
        {
            var size = ByteSize.FromBytes(sizeInBytes).Megabytes;
            // f(x) = 5 + (25/e^(0.25x - 8))
            var minutes = 5 + 25 / (1 + Math.Exp(0.25 * size - 8));
            return DateTimeOffset.Now.AddMinutes(minutes);
        }
    }
}