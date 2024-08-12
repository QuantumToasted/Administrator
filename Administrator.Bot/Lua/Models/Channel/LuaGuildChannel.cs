using Disqord;

namespace Administrator.Bot;

public abstract class LuaGuildChannel(IGuildChannel channel) : LuaChannel(channel)
{
    //public long GuildId { get; } = (long) channel.GuildId.RawValue;
    
    public string Mention { get; } = channel.Mention;

    public int Position { get; } = channel.Position;

    //public long Flags { get; } = (long) channel.Flags;
}