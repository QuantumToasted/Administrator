using Disqord;
using Disqord.Rest;
using Laylua;
using Laylua.Marshaling;
using Qommon;

namespace Administrator.Bot;

[LuaType]
public sealed partial class LuaVoiceChannel(IVoiceChannel channel) : LuaGuildChannel(channel)
{
    [LuaName("category")]
    public Snowflake? CategoryId { get; } = channel.CategoryId;

    public string? Region { get; } = channel.Region;

    public Snowflake? LastMessageId { get; } = channel.LastMessageId;

    public DateTimeOffset? LastPinTimestamp { get; } = channel.LastPinTimestamp;

    public TimeSpan Slowmode { get; } = channel.Slowmode;

    public string Tag { get; } = channel.Tag;

    public bool IsAgeRestricted { get; } = channel.IsAgeRestricted;

    public int MemberLimit { get; } = channel.MemberLimit;
    
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