using Disqord;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class RoleComponentModule
{
    [ModalCommand("Role:Modify:*")]
    public partial Task<IResult> Modify(Snowflake roleId, string name, Color? color = null, bool? hoisted = null, bool? mentionable = null);
}