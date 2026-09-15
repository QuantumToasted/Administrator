using Disqord;

namespace Administrator.Bot;

public sealed class LuaRole(IRole role)
{
    public Snowflake Id { get; } = role.Id;

    public string Name { get; } = role.Name;
    
    public string Mention { get; } = role.Mention;
    
    public string? Color { get; } = role.Color?.ToString();

    public string? Icon { get; } = role.UnicodeEmoji is { } emoji
        ? emoji.ToString()
        : !string.IsNullOrWhiteSpace(role.IconHash)
            ? role.GetIconUrl(CdnAssetFormat.Automatic, size: 512)
            : null;
    
    public bool Hoisted { get; } = role.IsHoisted;
    
    public int Position { get; } = role.Position;
    
    public long Permissions { get; } = (long) role.Permissions;
    
    public bool Managed { get; } = role.IsManaged;
    
    public bool Mentionable { get; } = role.IsMentionable;
}