using Disqord;

namespace Administrator.Bot;

public class LuaUser(IUser user) : ILuaModel<LuaUser>
{
    public long Id { get; } = (long) user.Id.RawValue;
    
    public string Name { get; } = user.Name;
    
    public string Mention { get; } = user.Mention;
    
    public string Tag { get; } = user.Tag;
    
    public string? Discriminator { get; } = user.Discriminator != "0000" ? user.Discriminator : null;
    
    public string? GlobalName { get; } = user.GlobalName;

    public string Avatar { get; } = user.GetAvatarUrl(CdnAssetFormat.Automatic, 1024);
    
    public bool Bot { get; } = user.IsBot;
    
    //public long Flags { get; } = (long) user.PublicFlags;

    public bool Member { get; } = user is IMember;
}