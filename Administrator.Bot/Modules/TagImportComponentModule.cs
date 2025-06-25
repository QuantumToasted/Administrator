using Disqord;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class TagImportComponentModule
{
    [ModalCommand("Tag:Import:*:*")]
    public partial Task<IResult> Import(Snowflake channelId, Snowflake messageId, string name);
}