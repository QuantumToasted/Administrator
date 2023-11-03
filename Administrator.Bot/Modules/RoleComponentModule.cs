using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Bot;

public sealed class RoleComponentModule : DiscordComponentGuildModuleBase
{
    [ModalCommand("Role:Modify:*")]
    public async Task<IResult> ModifyAsync(
        Snowflake roleId,
        string name,
        Color? color = null,
        bool? hoisted = null,
        bool? mentionable = null)
    {
        hoisted ??= false;
        mentionable ??= false;

        await Bot.ModifyRoleAsync(Context.GuildId, roleId, x =>
        {
            x.Name = name;
            x.Color = color;
            x.IsHoisted = hoisted.Value;
            x.IsMentionable = mentionable.Value;
        });

        return Response($"Role {Mention.Role(roleId)} modified.");
    }
}