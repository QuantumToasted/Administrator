using Disqord;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class UtilityComponentModule : DiscordComponentModuleBase
{
    public partial IResult DumpId(Snowflake id)
        => Response(id.ToString()).AsEphemeral();
}