using Disqord;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class ModerationComponentModule
{
    [ModalCommand("Ban:*")]
    public partial Task<IResult> Ban(Snowflake userId, string? reason = null, TimeSpan? duration = null, int? messagePruneDays = null);

    [ModalCommand("Warning:*")]
    public partial Task<IResult> Warn(Snowflake userId, string? reason = null, [Range(0, 50)] int? demeritPoints = null);

    [ModalCommand("Kick:*")]
    public partial Task<IResult> Kick(Snowflake userId, string? reason = null);

    [ModalCommand("Timeout:*")]
    public partial Task<IResult> Timeout(Snowflake userId, TimeSpan duration, string? reason = null);
}