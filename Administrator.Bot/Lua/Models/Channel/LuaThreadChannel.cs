using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Laylua;
using Laylua.Marshaling;
using Qommon;

namespace Administrator.Bot;

[LuaType]
public sealed partial class LuaThreadChannel(IThreadChannel channel) : LuaGuildChannel(channel)
{
    public Snowflake? LastMessageId { get; } = channel.LastMessageId;

    public DateTimeOffset? LastPinTimestamp { get; } = channel.LastPinTimestamp;
    
    public TimeSpan Slowmode { get; } = channel.Slowmode;
    
    public string Tag { get; } = channel.Tag;
    
    [LuaName("parent")]
    public Snowflake ParentId { get; } = channel.ChannelId;
    
    [LuaName("creator")]
    public Snowflake CreatorId { get; } = channel.CreatorId;
    
    public int MessageCount { get; } = channel.MessageCount;
    
    public string[] Tags { get; } = GetTags(channel);
    
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

    private static string[] GetTags(IThreadChannel channel)
    {
        var bot = (DiscordBotBase) channel.Client;
        return (bot.GetChannel(channel.GuildId, channel.ChannelId) as IForumChannel)?.Tags
            .Where(x => channel.TagIds.Contains(x.Id))
            .Select(x => x.Name).ToArray() ?? [];
    }
}