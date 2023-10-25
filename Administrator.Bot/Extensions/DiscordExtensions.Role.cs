using Disqord;
using Disqord.Gateway;

namespace Administrator.Bot;

public static partial class DiscordExtensions
{
    // TODO: Premium Membership roles
    public static bool CanBeGrantedOrRevoked(this IRole role)
        => role.Tags is { IsNitroBooster: false, BotId: null, IntegrationId: null };

    public static int GetOrderedPosition(this IRole role, out IRole? roleAbove, out IRole? roleBelow)
    {
        var client = (DiscordClientBase) role.Client;
        var roles = client.GetRoles(role.GuildId).Values.Where(x => x.Id != role.GuildId).ToList();

        roleAbove = roles.Where(x => x.Position > role.Position).MinBy(x => x.Position);
        roleBelow = roles.Where(x => x.Position < role.Position).MaxBy(x => x.Position);

        // 1-indexed
        return roles.Count - roles.OrderBy(x => x.Position)
            .Select(x => x.Id).ToList().IndexOf(role.Id);
    }
}