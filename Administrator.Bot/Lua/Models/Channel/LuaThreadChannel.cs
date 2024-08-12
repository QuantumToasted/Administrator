using Disqord;
using Disqord.Rest;
using Laylua;

namespace Administrator.Bot;

public sealed class LuaThreadChannel(IThreadChannel channel, DiscordLuaLibraryBase library) : LuaGuildChannel(channel), ILuaModel<LuaThreadChannel>
{
    public long? LastMessageId { get; } = (long?) channel.LastMessageId?.RawValue;
    
    public string? LastPinTimestamp { get; } = channel.LastPinTimestamp?.ToString("s");
    
    public string Slowmode { get; } = channel.Slowmode.ToString("g");
    
    public string Tag { get; } = channel.Tag;
    
    public long ParentId { get; } = (long) channel.ChannelId.RawValue;
    
    public long CreatorId { get; } = (long) channel.CreatorId.RawValue;
    
    public int MessageCount { get; } = channel.MessageCount;
    
    //public int MemberCount { get; } = channel.MemberCount;
    
    public long[] Tags { get; } = channel.TagIds.Select(x => (long) x.RawValue).ToArray();
    
    public void SetName(string name)
        => library.RunWait(ct => channel.ModifyAsync(x => x.Name = name, cancellationToken: ct));
    
    public void Delete()
        => library.RunWait(ct => channel.DeleteAsync(cancellationToken: ct));
    
    public long SendMessage(LuaTable msg)
    {
        var message = DiscordLuaLibraryBase.ConvertMessage<LocalMessage>(msg);
        var newMessage = library.RunWait(ct => channel.SendMessageAsync(message, cancellationToken: ct));
        return (long)newMessage.Id.RawValue;
    }
    
    public LuaMessage? GetMessage(long id)
    {
        var message = library.RunWait(async ct =>
        {
            try
            {
                var m = await channel.GetOrFetchMessageAsync((ulong)id, ct);
                return m;
            }
            catch
            {
                return null;
            }
        });

        return message is not null ? new LuaMessage(message, channel.GuildId, library) : null;
    }
}