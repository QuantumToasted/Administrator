using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class AppealComponentModule
{
    [ButtonCommand("Appeal:CreateModal:*")]
    public partial Task CreateAppealModal(int id);

    [ModalCommand("Appeal:*:*")]
    public partial Task<IResult> Appeal(Snowflake messageId, int id, string appeal);

    [ButtonCommand("Appeal:Accept:*")]
    [RequireGuild]
    public partial Task<IResult> Accept(int id);

    [ButtonCommand("Appeal:NeedsInfo:*")]
    [RequireGuild]
    public partial Task<IResult> NeedsInfo(int id);

    [ButtonCommand("Appeal:Reject:*")]
    [RequireGuild]
    public partial Task<IResult> Reject(int id);

    [ButtonCommand("Appeal:Ignore:*")]
    [RequireGuild]
    public partial Task<IResult> Ignore(int id);
}