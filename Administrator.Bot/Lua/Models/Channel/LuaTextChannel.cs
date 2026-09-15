using Disqord;
using Disqord.Rest;
using Laylua;
using Laylua.Marshaling;
using Qommon;

namespace Administrator.Bot;

[LuaType]
public sealed partial class LuaTextChannel(ITextChannel channel) : LuaGuildChannel(channel)
{
    [LuaName("lastMessage")]
    public Snowflake? LastMessageId { get; } = channel.LastMessageId;
    
    public string Tag { get; } = channel.Tag;
    
    public DateTimeOffset? LastPin { get; } = channel.LastPinTimestamp;
    
    public bool AgeRestricted { get; } = channel.IsAgeRestricted;
    
    public int Slowmode { get; } = (int) channel.Slowmode.TotalSeconds;
    
    public string? Topic { get; } = channel.Topic;
    
    [LuaName("category")]
    public Snowflake? CategoryId { get; } = channel.CategoryId;
    
    public bool News { get; } = channel.IsNews;
    
    public TimeSpan ArchiveThreadsAfter { get; } = channel.DefaultAutomaticArchiveDuration;

    public async Task<bool> SetTopic(string topic)
    {
        try
        {
            Guard.IsNotNull(topic);
            await channel.ModifyAsync(x => x.Topic = topic);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Snowflake?> SendMessage(LuaMessage msg)
    {
        try
        {
            var message = LocalMessage.CreateFrom(msg);
            var sentMessage = await channel.SendMessageAsync(message);
            return sentMessage.Id;
        }
        catch
        {
            return null;
        }
    }

    public async Task<LuaDiscordMessage?> GetMessage(Snowflake id)
    {
        try
        {
            var message = await channel.GetOrFetchMessageAsync(id);
            return message is not null
                ? new LuaDiscordMessage(message)
                : null;
        }
        catch
        {
            return null;
        }
    }
}