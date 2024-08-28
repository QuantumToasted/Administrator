using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("config")]
[RequireInitialAuthorPermissions(Permissions.ManageGuild)]
public sealed partial class ConfigModule
{
    [SlashCommand("join-role")]
    [Description("Sets or updates the server's join role, given to users upon joining the server.")]
    public partial Task<IResult> SetJoinRole(
        [Description("The new role. Supply no value to instead view the current join role.")]
            IRole? role = null,
        [Description("If True, disables the join role.")]
            bool disable = false);

    [SlashCommand("level-up-emoji")]
    [Description("Sets or updates the server's custom level-up emoji.")]
    public partial Task<IResult> SetLevelUpEmoji(
        [Description("The new emoji.")] 
            IEmoji emoji);
    
    [SlashGroup("logging-channels")]
    public sealed partial class LoggingChannelConfigModule
    {
        [SlashCommand("view")]
        [Description("Views currently configured logging channels.")]
        public partial Task<IResult> View();

        [SlashCommand("set")]
        [Description("Sets or updates what channel logs a specific event type.")]
        public partial Task<IResult> Set(
            [Description("The event type.")]
                LogEventType type,
            [Description("The channel to log it to.")]
            [ChannelTypes(ChannelType.Text)]
            [RequireBotChannelPermissions(Permissions.SendMessages)]
                IChannel channel);

        [SlashCommand("remove")]
        [Description("Disables event logging for a specific event type.")]
        public partial Task<IResult> Remove(
            [Description("The event type.")]
                LogEventType type);

        [SlashCommand("clear")]
        [Description("Clears all log events for a channel or the whole server.")]
        public partial Task Clear(
            [ChannelTypes(ChannelType.Text)]
                IChannel? channel = null);
    }

    [SlashGroup("settings")]
    public sealed partial class SettingConfigModule
    {
        [SlashCommand("view")]
        [Description("Displays a list of settings and whether they are enabled or disabled.")]
        public partial IResult View();

        [SlashCommand("enable")]
        [Description("Enables a specific server setting.")]
        public partial Task<IResult> Enable(
            [Description("The setting to enable.")]
                GuildSettingFlags setting);

        [SlashCommand("disable")]
        [Description("Disables a specific server setting.")]
        public partial Task<IResult> Disable(
            [Description("The setting to disable.")]
                GuildSettingFlags setting);
    }

    [SlashGroup("invite-filter-exemptions")]
    public sealed partial class InviteFilterExemptionConfigModule
    {
        [SlashCommand("add")]
        [Description("Adds a single exemption to the invite filter.")]
        public partial Task<IResult> Add(
            [Description("A user to exempt.")]
                IUser? user = null,
            [Description("A role to exempt.")]
                IRole? role = null,
            [Description("A channel to exempt.")]
            [ChannelTypes(ChannelType.Text, ChannelType.PublicThread)]
                IChannel? channel = null,
            [Name("server-id")]
            [Description("The ID of a server to exempt.")]
                Snowflake? guildId = null,
            [Description("A specific invite code to exempt.")]
                string? inviteCode = null);

        [SlashCommand("remove")]
        [Description("Removes a single exemption from the invite filter.")]
        public partial Task<IResult> Remove(
            [Description("A user to remove.")]
                IUser? user = null,
            [Description("A role to remove.")]
                IRole? role = null,
            [Description("A channel to remove.")]
            [ChannelTypes(ChannelType.Text, ChannelType.PublicThread)]
                IChannel? channel = null,
            [Name("server-id")]
            [Description("The ID of a server to remove.")]
                Snowflake? guildId = null,
            [Description("A specific invite code to remove.")]
                string? inviteCode = null);
    }

    [SlashGroup("automatic-punishments")]
    public sealed partial class AutomaticPunishmentConfigModule
    {
        [SlashCommand("list")]
        [Description("Lists existing automatic punishments.")]
        public partial Task<IResult> List();

        [SlashCommand("set")]
        [Description("Sets or updates an automatic punishment.")]
        public partial Task<IResult> Set(
            [Description("The number of demerit points the punishment will occur at or above.")]
            [Minimum(1)]
                int demeritPoints,
            [Description("The type of punishment to give.")]
                PunishmentTypeSelection type,
            [Description("(If Ban or Timeout) the duration of the punishment.")]
                TimeSpan? duration = null);

        [SlashCommand("remove")]
        [Description("Removes an automatic punishment.")]
        public partial Task<IResult> Remove(
            [Description("The number of demerit points that will no longer trigger an automatic punishment.")]
            [Minimum(1)]
                int demeritPoints);

        [AutoComplete("remove")]
        public partial Task AutoCompleteAutomaticPunishments(AutoComplete<int> demeritPoints);
    }

    [SlashGroup("xp-exempt-channels")]
    public sealed partial class XpExemptChannelConfigModule
    {
        [SlashCommand("add")]
        [Description("Exempts a channel from tracking/incrementing XP.")]
        public partial Task<IResult> Add(
            [Description("The channel to exempt.")]
            [ChannelTypes(ChannelType.Text, ChannelType.PublicThread)]
                IChannel channel);

        [SlashCommand("remove")]
        [Description("Removes a channel from the XP tracking exemption list.")]
        public partial Task<IResult> Remove(
            [Description("The channel to remove.")]
            [ChannelTypes(ChannelType.Text, ChannelType.PublicThread)]
                IChannel channel);
    }

    [SlashCommand("tag-limits")]
    [Description("Sets or updates the maximum number of tags users can create.")]
    public partial Task<IResult> SetTagLimits(
        [Description("The maximum number of tags per user. (-1 disables the limit.)")]
        [Minimum(-1)]
            int limit);

    [SlashGroup("auto-quote-exempt-channels")]
    public sealed partial class AutoQuoteExemptChannelConfigModule
    {
        [SlashCommand("add")]
        [Description("Exempts a channel's messages from triggering the auto-quote feature.")]
        public partial Task<IResult> Add(
            [Description("The channel to exempt.")]
            [ChannelTypes(ChannelType.Text, ChannelType.PublicThread)]
                IChannel channel);

        [SlashCommand("remove")]
        [Description("Removes a channel from the auto-quote exemption list.")]
        public partial Task<IResult> Remove(
            [Description("The channel to remove.")]
            [ChannelTypes(ChannelType.Text, ChannelType.PublicThread)]
                IChannel channel);
    }

    [SlashGroup("greeting")]
    public sealed partial class GreetingConfigModule
    {
        [SlashCommand("message")]
        [Description("Views, modifies, or removes the server's greeting message.")]
        public partial Task<IResult> ModifyMessage(
            Mode mode = Mode.Modify);

        [SlashCommand("dm")]
        [Description("Sets whether the greeting message should be sent in a user's DMs.")]
        public partial Task<IResult> ModifyGreetingDm(bool dm);
    }
    
    [SlashGroup("goodbye")]
    public sealed partial class GoodbyeConfigModule
    {
        [SlashCommand("message")]
        [Description("Views, modifies, or removes the server's goodbye message.")]
        public partial Task<IResult> ModifyMessage(
            Mode mode = Mode.Modify);
    }

    [SlashCommand("punishment-text")]
    [Description("Sets or updates the server's custom punishment text sent to users.")]
    public partial Task<IResult> SetPunishmentText(
        [Description("The new text. Whitespace only disables the custom text, or supply nothing to view the current text.")]
            string? text = null);

    [SlashCommand("xp-rate")]
    [Description("Sets or updates the server's custom XP rate.")]
    public partial Task<IResult> SetXpRate(
        [Description("The amount of XP to gain every 'interval'.")]
        [Range(1, 1000)]
            int amount,
        [Description("The minimum interval between messages granting XP.")]
            TimeSpan interval);

    [SlashCommand("default-ban-prune-days")]
    [Description("Sets the default number of days' worth of messages pruned by default via the /ban command.")]
    public partial Task<IResult> SetDefaultBanPruneDays(
        [Description("The number of days, or 0 to disable message pruning.")]
        [Range(0, 7)]
            int pruneDays);

    [SlashGroup("demerit-points")]
    public sealed partial class DemeritPointConfigModule
    {
        [SlashCommand("warning-default")]
        [Description("Sets the default number of demerit points given for warnings with none explicitly specified.")]
        public partial Task<IResult> SetWarningDefault(
            [Description("The number of demerit points to give, or 0 to give none.")]
            [Range(0, 50)]
                int demeritPoints);

        [SlashCommand("decay-interval")]
        [Description("The interval at which demerit points will decay.")]
        public partial Task<IResult> SetDecayInterval(
            [Description("The new interval. Supplying nothing will disable demerit points from decaying.")]
                TimeSpan? interval = null);
    }
}