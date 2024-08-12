using System.Text.Json.Serialization;
using Disqord;
using Disqord.Models;

namespace Administrator.Core;

public sealed record UserSnapshot(ulong Id, string Name, string? Discriminator, string? GlobalName, bool IsBot) : IUser
{
    [JsonIgnore]
    public string Mention => Disqord.Mention.User(Id);
    
    [JsonIgnore] 
    public string Tag => this.HasMigratedName() ? Name : $"{Name}#{Discriminator}";

    public override string ToString()
        => Tag;

    public static UserSnapshot FromUser(IUser user)
    {
        var discriminator = user.Discriminator != "0000" ? user.Discriminator : null;
        return new UserSnapshot(user.Id, user.Name, discriminator, user.GlobalName, user.IsBot);
    }

    Snowflake IIdentifiableEntity.Id => Id;
    string IUser.Discriminator => Discriminator ?? "0000";
    string IUser.AvatarHash => throw new InvalidOperationException(); // Avatars are prone to deletion, making this useless
    UserFlags IUser.PublicFlags => throw new InvalidOperationException();
    IClient IClientEntity.Client => throw new InvalidOperationException();
    void IJsonUpdatable<UserJsonModel>.Update(UserJsonModel model) => throw new InvalidOperationException();
}