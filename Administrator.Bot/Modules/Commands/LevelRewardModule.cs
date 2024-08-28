using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("level-reward")]
[RequireInitialAuthorPermissions(Permissions.ManageGuild)]
public sealed partial class LevelRewardModule
{
    [SlashCommand("list")]
    [Description("Lists all level rewards in this server.")]
    public partial Task<IResult> List();

    [SlashCommand("clear")]
    [Description("Clears all level rewards in this server.")]
    public partial Task Clear(
        [Description("If set, only clears level rewards for this tier.")]
        [Minimum(1)]
            int? tier = null);

    [SlashCommand("add")]
    [Description("Adds a new (or replaces an existing) role level reward for this server.")]
    public partial Task Add(
        [Description("The tier this reward is for. (Tiers reset every 150 levels.)")]
        [Minimum(1)]
            int tier,
        [Description("The level (2-150) this reward is for.")]
        [Range(2, 150)]
            int level,
        [Name("added-roles")]
        [Description("A comma-separated list of roles to give to the member upon leveling up.")]
            string? grantedRoleStr = null,
        [Name("removed-roles")]
        [Description("A comma-separated list of roles to remove the member upon leveling up.")]
            string? revokedRoleStr = null);

    [SlashCommand("remove")]
    [Description("Removes an existing role level reward from this server.")]
    public partial Task Remove(
        [Description("The tier this role level reward is for.")]
        [Minimum(1)]
            int tier,
        [Description("The level (2-150) in the tier this role level reward is for.")]
        [Range(2, 150)]
            int level);

    [AutoComplete("remove")]
    public partial Task AutoCompleteLevelRewards(AutoComplete<int> tier, AutoComplete<int> level);
}