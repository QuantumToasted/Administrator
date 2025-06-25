using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("self")]
public sealed partial class SelfModule
{
    [SlashCommand("timezone")]
    [Description("Sets your timezone. Used for specifying and converting your local time into UTC.")]
    public partial Task<IResult> Timezone(
        [Description("The new timezone. (Pick the location closest to you.)")]
            TimeZoneInfo timezone);

    [SlashCommand("demerit-points")]
    [Description("Views your current demerit points in a server.")]
    public partial Task<IResult> DemeritPoints(
        [Name("server-id")]
        [Description("The ID of the server to view demerit points for. Defaults to the current server.")]
            Snowflake? guildId = null);

    [SlashCommand("punishments")]
    [Description("Views your current punishments in a server.")]
    public partial Task<IResult> Punishments(
        [Name("server-id")]
        [Description("The ID of the server to view punishments for. Defaults to the current server.")]
            Snowflake? guildId = null);

    [AutoComplete("timezone")]
    public partial void AutoCompleteTimezones(AutoComplete<string> timezone);
}