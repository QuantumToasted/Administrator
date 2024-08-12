using Disqord;

namespace Administrator.Bot;

public sealed class LuaGuildEmoji(IGuildEmoji emoji, DiscordLuaLibraryBase library) : ILuaModel<LuaGuildEmoji>
{
    public long Id { get; } = (long) emoji.Id.RawValue;
    
    public string Tag { get; } = emoji.Tag;
    
    public bool Animated { get; } = emoji.IsAnimated;
    
    //public long GuildId { get; } = (long) emoji.GuildId.RawValue;
    
    public string Name { get; } = emoji.Name;
    
    public long[] RoleIds { get; } = emoji.RoleIds.Select(x => (long) x.RawValue).ToArray();

    public LuaUser? Creator { get; } = emoji.Creator switch
    {
        IMember member => new LuaMember(member, library),
        not null => new LuaUser(emoji.Creator),
        _ => null
    };
    
    //public bool RequiresColons { get; } = emoji.RequiresColons;
    
    public bool Managed { get; } = emoji.IsManaged;
    
    //public bool IsAvailable { get; } = emoji.IsAvailable;
}