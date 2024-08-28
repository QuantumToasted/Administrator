using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class RoleComponentModule : DiscordComponentGuildModuleBase
{
    public partial async Task<IResult> Modify(Snowflake roleId, string name, Color? color, bool? hoisted, bool? mentionable)
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