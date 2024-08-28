using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("punishments")]
[RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
public sealed partial class PunishmentsModule
{
    [SlashCommand("demerit-points")]
    public partial Task<IResult> DemeritPoints(IUser user);
    
    [SlashGroup("for")]
    public sealed partial class PunishmentsForModule
    {
        [SlashCommand("target")]
        [Description("Lists all punishments with a specific user as the target.")]
        public partial Task<IResult> Target(
            [Description("The user who was the target of the punishment(s).")]
                IUser user);
    }

    [SlashGroup("from")]
    public sealed partial class PunishmentsFromModule
    {
        [SlashCommand("moderator")]
        [Description("Lists all punishments with a specific user as the moderator.")]
        public partial Task<IResult> Moderator(
            [Description("The user who was the moderator of the punishment(s).")]
                IUser user);
    }

    [SlashCommand("case")]
    [Description("Views detailed information about a single punishment case in this server.")]
    public partial Task<IResult> Case(
        [Description("The ID of the punishment to view.")]
            int id);

    [AutoComplete("case")]
    public partial Task AutoCompleteAllPunishments(AutoComplete<int> id);
}