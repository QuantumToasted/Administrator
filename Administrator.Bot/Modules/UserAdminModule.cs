using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("user-admin")]
[RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
public sealed partial class UserAdminModule
{
    [SlashCommand("search")]
    [Description("Searches for users matching your input.")]
    [RequireGuild]
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
}