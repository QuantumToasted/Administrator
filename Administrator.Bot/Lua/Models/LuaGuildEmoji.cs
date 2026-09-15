using Disqord;
using Laylua.Marshaling;

namespace Administrator.Bot;

public sealed class LuaGuildEmoji(IGuildEmoji emoji)
{
    public Snowflake Id { get; } = emoji.Id;
    
    public string Tag { get; } = emoji.Tag;
    
    [LuaName("animated")]
    public bool IsAnimated { get; } = emoji.IsAnimated;
    
    public string Name { get; } = emoji.Name;
    
    public Snowflake[] RoleIds { get; } = emoji.RoleIds.ToArray();

    public LuaUser? Creator { get; } = emoji.Creator switch
    {
        not null => LuaUser.FromUser(emoji.Creator),
        _ => null
    };
    
    [LuaName("managed")]
    public bool IsManaged { get; } = emoji.IsManaged;
}