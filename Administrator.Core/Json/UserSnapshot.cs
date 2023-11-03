using Disqord;
using Disqord.Models;

namespace Administrator.Core;

public sealed record UserSnapshot(Snowflake Id, string Name, string? Discriminator, string? GlobalName, bool IsBot) : IUser
{
    public string Mention => Disqord.Mention.User(Id);
    
    public string Tag => this.HasMigratedName() ? Name : $"{Name}#{Discriminator}";

    public override string ToString()
        => Tag;

    public static UserSnapshot FromUser(IUser user)
    {
        var discriminator = user.Discriminator != "0000" ? user.Discriminator : null;
        return new UserSnapshot(user.Id, user.Name, discriminator, user.GlobalName, user.IsBot);
    }

    string IUser.Discriminator => Discriminator ?? "0000";
    string? IUser.AvatarHash => throw new InvalidOperationException(); // Avatars are prone to deletion, making this useless
    UserFlags IUser.PublicFlags => throw new InvalidOperationException();
    IClient IClientEntity.Client => throw new InvalidOperationException();
    void IJsonUpdatable<UserJsonModel>.Update(UserJsonModel model) => throw new InvalidOperationException();
}