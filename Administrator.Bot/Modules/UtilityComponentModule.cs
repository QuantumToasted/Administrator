using Disqord;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class UtilityComponentModule
{
    [ButtonCommand("DumpId:*")]
    public partial IResult DumpId(Snowflake id);
}