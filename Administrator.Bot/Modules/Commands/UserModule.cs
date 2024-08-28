using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("user")]
public sealed partial class UserModule
{
    /*
    [SlashCommand("search")]
    [Description("Searches for users matching your input.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    public partial Task<IResult> Search(
        [Description("The input text. If regex is False, searches using a weighted Levenshtein distance.")]
        [Maximum(50)]
            string text,
        [Description("Whether to search using regular expressions (regex). Default: False")]
            bool regex = false,
        [Description("Whether to include usernames in the search. Default: True")]
            bool usernames = true, 
        [Description("Whether to include nicknames in the search. Default: True")]
            bool nicknames = true, 
        [Description("Whether to include global names in the search. Default: True")]
            bool globalNames = true,
        [Description("The maximum allowed Levenshtein distance (0-10) if regex is False. Default: 2")]
        [Range(0, 10)]
            int maxDistance = 2);
    */

    [SlashCommand("info")]
    [Description("Displays information for a user or member.")]
    public partial Task<IResult> Info(
        [Description("The user/member to display information for. Defaults to yourself.")]
            IUser? user = null);

    [SlashCommand("server-avatar")]
    [Description("Displays a member's server avatar.")]
    [RequireGuild]
    public partial Task<IResult> ServerAvatar(
        [Description("The member whose server avatar is being displayed. Defaults to yourself.")]
            IMember? member);

    [SlashCommand("avatar")]
    [Description("Displays a user or member's global avatar.")]
    public partial Task<IResult> Avatar(
        [Description("The user/member whose avatar is being displayed. Defaults to yourself.")]
            IUser? user = null);

    [SlashGroup("xp")]
    public sealed partial class UserXpModule
    {
        [SlashCommand("stats")]
        [Description("Displays global and server XP statistics for a user or member.")]
        public partial Task<IResult> Stats(
            [Description("The user/member to display statistics for. Defaults to yourself.")]
            [RequireNotBot]
                IUser? user = null);

        [RequireGuild]
        [SlashCommand("leaderboard")]
        [Description("Displays an XP leaderboard for this server.")]
        public partial Task<IResult> Leaderboard(
            [Description("Whether to start on the page your rank is on. Default: False")]
                bool startWithSelf = false);

        [RequireGuild]
        [SlashCommand("blurb")]
        [Description("Updates your personal \"blurb\" text for this server.")]
        public partial Task<IResult> Blurb(
            [Description("The new text to set. Will only be displayed in this server.")]
            [Range(1, 50)]
                string text);
    }
}