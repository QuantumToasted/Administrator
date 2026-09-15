using Disqord;
using Disqord.Models;
using Laylua.Marshaling;

namespace Administrator.Bot;

[LuaType]
public partial class LuaUser(IUser user) : IUser
{
    public Snowflake Id { get; } = user.Id;
    
    public string Name { get; } = user.Name;
    
    public string Mention { get; } = user.Mention;
    
    public string Tag { get; } = user.Tag;
    
    public string Discriminator { get; } = user.Discriminator;
    
    public string? GlobalName { get; } = user.GlobalName;

    public string Avatar { get; } = user.GetAvatarUrl(CdnAssetFormat.Automatic, 512);
    
    public bool IsBot { get; } = user.IsBot;

    public bool Member { get; } = user is IMember;

    public static LuaUser FromUser(IUser user)
    {
        return user is IMember member ? new LuaMember(member) : new LuaUser(user);
    }

    public UserFlags PublicFlags => user.PublicFlags;

    IUserPrimaryGuild? IUser.PrimaryGuild => user.PrimaryGuild;
    IAvatarDecoration? IUser.AvatarDecoration => user.AvatarDecoration;
    ICollectibles? IUser.Collectibles => user.Collectibles;

    public string? AvatarHash => user.AvatarHash;
    public IClient Client => user.Client;
    public void Update(UserJsonModel model) => user.Update(model);
}