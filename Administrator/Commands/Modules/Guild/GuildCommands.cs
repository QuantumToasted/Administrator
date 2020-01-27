using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Disqord.Rest;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Permission = Disqord.Permission;

namespace Administrator.Commands
{
    [Name("Guild")]
    [Group("guild", "server")]
    [RequireContext(ContextType.Guild)]
    public class GuildCommands : AdminModuleBase
    {
        public PaginationService Pagination { get; set; }

        public CommandHandlerService CommandHandler { get; set; }

        public IServiceProvider Provider { get; set; }

        public Random Random { get; set; }

        [RequireUserPermissions(Permission.ManageGuild)]
        public class GuildManagementCommands : GuildCommands
        {
            [Command("language")]
            public async ValueTask<AdminCommandResult> GetLanguageAsync()
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                return CommandSuccessLocalized("guild_language", args:
                    $"{Markdown.Bold(guild.Language.NativeName)} ({guild.Language.EnglishName}, `{guild.Language.CultureCode}`)");
            }

            [Command("language")]
            public async ValueTask<AdminCommandResult> SetLanguageAsync([Remainder] LocalizedLanguage newLanguage)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                guild.Language = newLanguage;
                Context.Database.Guilds.Update(guild);
                await Context.Database.SaveChangesAsync();
                Context.Language = newLanguage;

                CommandHandler.UpdateLanguage(Context.Guild.Id, newLanguage);

                return CommandSuccessLocalized("guild_language_set", args:
                    $"{Markdown.Bold(guild.Language.NativeName)} ({guild.Language.EnglishName}, `{guild.Language.CultureCode}`)");
            }

            [Command("languages")]
            public AdminCommandResult GetLanguages()
                => CommandSuccess(new StringBuilder()
                    .AppendNewline(Localize("available_languages"))
                    .AppendJoin('\n',
                        Localization.Languages.Select(
                            x => Markdown.Code($"{x.NativeName} ({x.EnglishName}, {x.CultureCode})"))).ToString());

            [Group("settings")]
            public sealed class GuildSettingsCommands : GuildManagementCommands
            {
                [Command]
                public async ValueTask<AdminCommandResult> ListGuildSettingsAsync()
                {
                    var builder =
                        new StringBuilder(Localize("guild_settings", Markdown.Bold(Context.Guild.Name.Sanitize())))
                            .AppendNewline()
                            .AppendNewline();

                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    foreach (var value in Enum.GetValues(typeof(GuildSettings)).Cast<GuildSettings>()
                        .Where(x => !x.Equals(default)))
                    {
                        var enabled = guild.Settings.HasFlag(value);
                        builder.Append($"`{value:G}` - ")
                            .AppendNewline(Localize($"info_{(enabled ? "enabled" : "disabled")}"));
                    }

                    return CommandSuccess(builder.ToString());
                }

                [Command("enable", "disable")]
                public async ValueTask<AdminCommandResult> EnableSettingAsync(GuildSettings setting)
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    var enabled = Context.Path[2].Equals("enable");

                    guild.Settings = enabled ? guild.Settings | setting : guild.Settings & ~setting;
                    Context.Database.Guilds.Update(guild);
                    await Context.Database.SaveChangesAsync();

                    return CommandSuccessLocalized(enabled ? "guild_settings_enabled" : "guild_settings_disabled",
                        args: Markdown.Code(setting.ToString("G")));
                }
            }

            [Group("prefixes", "prefix")]
            public sealed class GuildManagementPrefixCommands : GuildManagementCommands
            {
                [Command("", "list")]
                public async ValueTask<AdminCommandResult> ListPrefixesAsync()
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    if (guild.CustomPrefixes.Count == 0)
                        return CommandErrorLocalized("guild_prefixes_none");

                    return CommandSuccess(
                        new StringBuilder(Localize("guild_prefixes", Markdown.Bold(Context.Guild.Name.Sanitize())))
                            .AppendNewline()
                            .AppendNewline()
                            .AppendJoin('\n', guild.CustomPrefixes.Select(x => $"\"{x.Sanitize()}\"")).ToString());
                }

                [Command("add")]
                public async ValueTask<AdminCommandResult> AddPrefixAsync([MustBe(StringLength.ShorterThan, 32)] string prefix)
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    if (guild.CustomPrefixes.Count == Database.Guild.MAX_CUSTOM_PREFIXES)
                        return CommandErrorLocalized("guild_prefixes_maximum");

                    if (guild.CustomPrefixes.Contains(prefix, StringComparer.OrdinalIgnoreCase))
                        return CommandErrorLocalized("guild_prefixes_exists");

                    guild.CustomPrefixes.Add(prefix);
                    Context.Database.Guilds.Update(guild);
                    await Context.Database.SaveChangesAsync();

                    CommandHandler.UpdatePrefixes(Context.Guild.Id, guild.CustomPrefixes);

                    return CommandSuccessLocalized("guild_prefixes_added", args: $"\"{prefix.Sanitize()}\"");
                }

                [Command("remove")]
                public async ValueTask<AdminCommandResult> RemovePrefixAsync([MustBe(StringLength.ShorterThan, 32)] string prefix)
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);

                    if (!guild.CustomPrefixes.Remove(prefix))
                        return CommandErrorLocalized("guild_prefixes_notfound");

                    Context.Database.Guilds.Update(guild);
                    await Context.Database.SaveChangesAsync();

                    CommandHandler.UpdatePrefixes(Context.Guild.Id, guild.CustomPrefixes);

                    return CommandSuccessLocalized("guild_prefixes_removed", args: $"\"{prefix.Sanitize()}\"");
                }

                [Command("clear")]
                public async ValueTask<AdminCommandResult> ClearPrefixesAsync()
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    if (guild.CustomPrefixes.Count == 0)
                        return CommandErrorLocalized("guild_prefixes_none");

                    guild.CustomPrefixes.Clear();
                    Context.Database.Guilds.Update(guild);
                    await Context.Database.SaveChangesAsync();

                    CommandHandler.UpdatePrefixes(Context.Guild.Id, guild.CustomPrefixes);

                    return CommandSuccessLocalized("guild_prefixes_cleared");
                }
            }

            [Command("xprate")]
            public async ValueTask<AdminCommandResult> GetXpRateAsync()
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                return CommandSuccessLocalized("guild_xp",
                    args: new object[]
                    {
                        Markdown.Bold(guild.XpRate.ToString()),
                        Markdown.Bold(guild.XpGainInterval.HumanizeFormatted(Localization, Context.Language))
                    });
            }

            [Command("xprate")]
            public async ValueTask<AdminCommandResult> GetXpRateAsync([MustBe(Operator.GreaterThan, 0)] int rate, [Remainder] TimeSpan interval)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                guild.XpRate = rate;
                guild.XpGainInterval = interval;
                Context.Database.Guilds.Update(guild);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("guild_xp_set", args: new object[]
                {
                    Markdown.Bold(guild.XpRate.ToString()),
                    Markdown.Bold(guild.XpGainInterval.HumanizeFormatted(Localization, Context.Language))
                });
            }

            [Command("levelwhitelist")]
            public async ValueTask<AdminCommandResult> SetLevelWhitelistAsync(
                params LevelUpNotification[] notifications)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                if (notifications.Length == 0)
                {
                    return CommandSuccess(FormatWhitelist("guild_levelwhitelist"));
                }
                
                guild.LevelUpWhitelist = LevelUpNotification.None;
                if (notifications.All(x => x != LevelUpNotification.None))
                {
                    foreach (var notification in notifications)
                    {
                        guild.LevelUpWhitelist |= notification;
                    }
                }

                Context.Database.Guilds.Update(guild);
                await Context.Database.SaveChangesAsync();

                return CommandSuccess(FormatWhitelist("guild_levelwhitelist_set"));

                string FormatWhitelist(string key)
                {
                    var builder =
                        new StringBuilder(Localize(key, Markdown.Bold(Context.Guild.Name.Sanitize())))
                            .AppendNewline()
                            .AppendNewline();

                    foreach (var value in Enum.GetValues(typeof(LevelUpNotification)).Cast<LevelUpNotification>()
                        .Where(x => !x.Equals(LevelUpNotification.None)))
                    {
                        var enabled = guild.LevelUpWhitelist.HasFlag(value);
                        builder.Append($"`{value:G}` - ")
                            .AppendNewline(Localize($"info_{(enabled ? "enabled" : "disabled")}"));
                    }

                    return builder.ToString();
                }
            }

            [Group("specialemoji")]
            public sealed class SpecialEmojiCommands : GuildManagementCommands
            {
                [Command("set")]
                public async Task<AdminCommandResult> SetSpecialEmojiAsync(EmojiType type, [RequireUsableEmoji] IEmoji emoji)
                {
                    if (await Context.Database.SpecialEmojis.FindAsync(Context.Guild.Id.RawValue, type) is { } specialEmoji)
                    {
                        specialEmoji.Emoji = emoji;
                        Context.Database.SpecialEmojis.Update(specialEmoji);
                    }
                    else
                    {
                        Context.Database.SpecialEmojis.Add(new SpecialEmoji(Context.Guild.Id, type, emoji));
                    }

                    await Context.Database.SaveChangesAsync();
                    return CommandSuccessLocalized("guild_levelemoji_set",
                        args: new object[] {Markdown.Code(type.ToString()), emoji.ToString()});
                }

                [Command("", "list")]
                public async Task<AdminCommandResult> ListSpecialEmojisAsync()
                {
                    var builder =
                        new StringBuilder(Localize("guild_emojis_list", Markdown.Bold(Context.Guild.Name.Sanitize())))
                            .AppendNewline()
                            .AppendNewline();

                    var specialEmojis = await Context.Database.SpecialEmojis.Where(x => x.GuildId == Context.Guild.Id)
                        .ToListAsync();

                    foreach (var value in Enum.GetValues(typeof(EmojiType)).Cast<EmojiType>()
                        .Where(x => !x.Equals(default)))
                    {
                        builder.Append($"`{value:G}` - ");

                        builder.AppendNewline(specialEmojis.FirstOrDefault(x => x.Type == value) is { } specialEmoji
                            ? specialEmoji.ToString()
                            : Localize("info_none"));
                    }

                    return CommandSuccess(builder.ToString());
                }
            }

            [Group("levelrewards")]
            public class LevelRewardCommands : GuildManagementCommands
            {
                [Command("", "list")]
                public async ValueTask<AdminCommandResult> ListLevelRewardsAsync()
                {
                    var rewards = await Context.Database.LevelRewards
                        .Where(x => x.GuildId == Context.Guild.Id)
                        .OrderBy(x => x.Tier)
                        .ThenBy(x => x.Level)
                        .ToListAsync();

                    if (rewards.Count == 0)
                        return CommandErrorLocalized("guild_levelrewards_list_none");

                    var pages = DefaultPaginator.GeneratePages(rewards, 5, FormatReward,
                        builderFunc: () => new LocalEmbedBuilder().WithSuccessColor().WithTitle(
                            Localize("guild_levelrewards_list", Markdown.Bold(Context.Guild.Name.Sanitize()))));

                    if (pages.Count > 1)
                    {
                        await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                        return CommandSuccess();
                    }

                    return CommandSuccess(embed: pages[0].Embed);

                    LocalEmbedFieldBuilder FormatReward(LevelReward reward)
                    {
                        var builder = new LocalEmbedFieldBuilder()
                            .WithName(Localize("levelreward_title", reward.Tier, reward.Level));
                        switch (reward)
                        {
                            case RoleLevelReward roleReward:
                                var addedRoles = string.Join(", ",
                                    roleReward.AddedRoleIds.Select(x => Context.Guild.GetRole(x).Format()));
                                var removedRoles = string.Join(", ",
                                    roleReward.RemovedRoleIds.Select(x => Context.Guild.GetRole(x).Format()));
                                return builder.WithValue(new StringBuilder(Localize("levelreward_role_addedroles"))
                                    .Append(" ")
                                    .AppendNewline(string.IsNullOrWhiteSpace(addedRoles)
                                        ? Localize("info_none")
                                        : addedRoles)
                                    .Append(Localize("levelreward_role_removedroles"))
                                    .Append(" ")
                                    .AppendNewline(string.IsNullOrWhiteSpace(removedRoles)
                                        ? Localize("info_none")
                                        : removedRoles).ToString()
                                    .TrimTo(LocalEmbedBuilder.MAX_DESCRIPTION_LENGTH / 2));
                            default:
                                throw new ArgumentOutOfRangeException(nameof(reward));
                        }
                    }
                }

                [Group("add")]
                public sealed class LevelRewardAddCommands : LevelRewardCommands
                {
                    [Command("role"), RunMode(RunMode.Parallel)]
                    public ValueTask<AdminCommandResult> AddOrModifyRoleLevelReward(
                        [MustBe(Operator.GreaterThan, 1)] int level)
                        => AddOrModifyRoleLevelRewardAsync(1, level);
                    

                    [Command("role"), RunMode(RunMode.Parallel)]
                    public async ValueTask<AdminCommandResult> AddOrModifyRoleLevelRewardAsync(
                        [MustBe(Operator.GreaterThan, 1)] [MustBe(Operator.LessThan, 8)]
                        int tier, [MustBe(Operator.GreaterThan, 1)] int level)
                    {
                        if (await Context.Database.LevelRewards.OfType<RoleLevelReward>()
                            .FirstOrDefaultAsync(x => x.Level == level && x.Tier == tier) is { } reward)
                        {
                            var addedRoles = string.Join('\n',
                                reward.AddedRoleIds.Select(x => Context.Guild.GetRole(x).Format()));
                            var removedRoles = string.Join('\n',
                                reward.RemovedRoleIds.Select(x => Context.Guild.GetRole(x).Format()));

                            var message = await Context.Channel.SendMessageAsync(
                                Localize("guild_levelrewards_modify_role", Markdown.Code("add"),
                                    Markdown.Code("remove")), embed: new LocalEmbedBuilder()
                                    .WithSuccessColor()
                                    .WithTitle(Localize("levelreward_title", reward.Tier, reward.Level))
                                    .AddField(Localize("levelreward_role_addedroles"),
                                        string.IsNullOrWhiteSpace(addedRoles) ? Localize("info_none") : addedRoles)
                                    .AddField(Localize("levelreward_role_removedroles"),
                                        string.IsNullOrWhiteSpace(removedRoles) ? Localize("info_none") : removedRoles)
                                    .Build());

                            var response = await GetNextMessageAsync();
                            bool isAdd;
                            switch (response?.Content?.ToLowerInvariant())
                            {
                                case "add":
                                    isAdd = true;
                                    break;
                                case "remove":
                                    isAdd = false;
                                    break;
                                default:
                                    return CommandErrorLocalized("info_timeout");
                            }

                            _ = response.DeleteAsync();

                            await message.ModifyAsync(x =>
                                x.Content = Localize(isAdd
                                    ? "guild_levelrewards_modify_role_add"
                                    : "guild_levelrewards_modify_role_remove"));

                            response = await GetNextMessageAsync();
                            if (response is null)
                                return CommandErrorLocalized("info_timeout");

                            var result =
                                await new RoleParser().ParseAsync(null, response.Content ?? string.Empty, Context);

                            if (!result.IsSuccessful)
                                return CommandError(result.Reason);

                            if (isAdd)
                            {
                                // slower :/
                                reward.AddedRoleIds = reward.AddedRoleIds.Append(result.Value.Id.RawValue)
                                    .Distinct().ToList();
                                addedRoles = string.Join('\n',
                                    reward.AddedRoleIds.Select(x => Context.Guild.GetRole(x).Format()));
                            }
                            else
                            {
                                // slower :/
                                reward.RemovedRoleIds = reward.RemovedRoleIds.Append(result.Value.Id.RawValue)
                                    .Distinct().ToList();
                                removedRoles = string.Join('\n',
                                    reward.RemovedRoleIds.Select(x => Context.Guild.GetRole(x).Format()));
                            }

                            Context.Database.LevelRewards.Update(reward);
                            await Context.Database.SaveChangesAsync();

                            _ = response.DeleteAsync();

                            await message.ModifyAsync(x =>
                            {
                                x.Content = Localize("guild_levelrewards_modify_role_success");
                                x.Embed = new LocalEmbedBuilder()
                                    .WithSuccessColor()
                                    .WithTitle(Localize("levelreward_title", reward.Tier, reward.Level))
                                    .AddField(Localize("levelreward_role_addedroles"),
                                        string.IsNullOrWhiteSpace(addedRoles) ? Localize("info_none") : addedRoles)
                                    .AddField(Localize("levelreward_role_removedroles"),
                                        string.IsNullOrWhiteSpace(removedRoles) ? Localize("info_none") : removedRoles)
                                    .Build();
                            });

                            return CommandSuccess();
                        }

                        Context.Database.LevelRewards.Add(new RoleLevelReward(Context.Guild.Id, level, tier,
                            Array.Empty<IRole>(), Array.Empty<IRole>()));
                        await Context.Database.SaveChangesAsync();
                        return CommandSuccessLocalized("guild_levelrewards_add_role");
                    }

                }

                [Command("remove")]
                public ValueTask<AdminCommandResult> RemoveLevelRewards(
                    [MustBe(Operator.GreaterThan, 1)] int level)
                    => RemoveLevelRewardsAsync(1, level);

                [Command("remove")]
                public async ValueTask<AdminCommandResult> RemoveLevelRewardsAsync(
                    [MustBe(Operator.GreaterThan, 1)] [MustBe(Operator.LessThan, 8)]
                    int tier, [MustBe(Operator.GreaterThan, 1)] int level)
                {
                    var rewards = await Context.Database.LevelRewards
                        .Where(x => x.GuildId == Context.Guild.Id && x.Level == level && x.Tier == tier)
                        .ToListAsync();

                    if (rewards.Count == 0)
                        return CommandErrorLocalized("guild_levelrewards_list_none");

                    Context.Database.LevelRewards.RemoveRange(rewards);
                    await Context.Database.SaveChangesAsync();

                    return CommandSuccessLocalized("guild_levelrewards_remove");
                }
            }

            [Command("specialrole")]
            public async ValueTask<AdminCommandResult> SetSpecialRoleAsync(RoleType type, [Remainder] CachedRole role)
            {
                if (await Context.Database.SpecialRoles.FindAsync(Context.Guild.Id.RawValue, type) is { } specialRole)
                {
                    specialRole.Id = role.Id;
                    Context.Database.SpecialRoles.Update(specialRole);
                }
                else
                {
                    Context.Database.SpecialRoles.Add(new SpecialRole(role.Id, Context.Guild.Id, type));
                }

                await Context.Database.SaveChangesAsync();
                return CommandSuccessLocalized("guild_specialrole_set", args: new object[] {Markdown.Bold(type.ToString()), role.Format()});
            }

            [Command("specialrole")]
            public async ValueTask<AdminCommandResult> GetSpecialRoleAsync(RoleType type)
            {
                if (!(await Context.Database.GetSpecialRoleAsync(Context.Guild.Id, type) is { } role))
                    return CommandErrorLocalized("guild_specialrole_none", args: Markdown.Bold(type.ToString()));

                return CommandSuccessLocalized("guild_specialrole", args: new object[] { Markdown.Bold(type.ToString()), role.Format() });
            }

            [Group("greeting")]
            public sealed class GreetingCommands : GuildManagementCommands
            {
                [Command]
                public async ValueTask<AdminCommandResult> TriggerAsync()
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    if (string.IsNullOrWhiteSpace(guild.Greeting))
                        return CommandErrorLocalized("guild_greeting_empty");

                    var channel = await Context.Database.GetLoggingChannelAsync(Context.Guild.Id,
                        LogType.Greeting);

                    var greeting = await guild.Greeting.FormatPlaceHoldersAsync(
                        AdminCommandContext.MockContext(Context.Language, Provider, Context.User, Context.Guild), Context.User.Mention, Random);

                    JsonEmbed embed;

                    if (!guild.DmGreeting)
                    {
                        if (channel is null)
                            return CommandErrorLocalized("guild_greeting_nochannel");

                        IUserMessage message;
                        if (JsonEmbed.TryParse(greeting, out embed))
                        {
                            message = await channel.SendMessageAsync(embed.Text ?? string.Empty, embed: embed.ToLocalEmbed());
                        }
                        else
                        {
                            message = await channel.SendMessageAsync(greeting);
                        }

                        if (guild.GreetingDuration.HasValue)
                        {
                            _ = Task.Run(async () =>
                                await Task.Delay(guild.GreetingDuration.Value)
                                    .ContinueWith(_ => message.DeleteAsync()));
                        }

                        await Context.Message.AddReactionAsync(EmojiTools.Checkmark);
                        return CommandSuccess();
                    }

                    if (JsonEmbed.TryParse(greeting, out embed))
                    {
                        try
                        {
                            await Context.User.SendMessageAsync(embed.Text ?? string.Empty,
                                embed: embed.ToLocalEmbed());
                            await Context.Message.AddReactionAsync(EmojiTools.Checkmark);
                        }
                        catch (DiscordHttpException ex) when (ex.JsonErrorCode ==
                                                              JsonErrorCode.CannotSendMessagesToThisUser)
                        {
                            await Context.Message.AddReactionAsync(EmojiTools.X);
                        }
                    }
                    else
                    {
                        try
                        {
                            await Context.User.SendMessageAsync(greeting);
                            await Context.Message.AddReactionAsync(EmojiTools.Checkmark);
                        }
                        catch (DiscordHttpException ex) when (ex.JsonErrorCode ==
                                                              JsonErrorCode.CannotSendMessagesToThisUser)
                        {
                            await Context.Message.AddReactionAsync(EmojiTools.X);
                        }
                    }

                    return CommandSuccess();
                }

                [Command]
                public async ValueTask<AdminCommandResult> SetTextAsync([Remainder] string text)
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    guild.Greeting = text;

                    Context.Database.Guilds.Update(guild);
                    await Context.Database.SaveChangesAsync();

                    return text is null // see DisableTextAsync()
                        ? CommandSuccessLocalized("guild_greeting_disabled")
                        : CommandSuccessLocalized("guild_greeting_updated");
                }

                [Command("disable")]
                public ValueTask<AdminCommandResult> DisableTextAsync()
                    => SetTextAsync(null);

                [Command("channel")]
                public async ValueTask<AdminCommandResult> GetChannelAsync()
                {
                    if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.Greeting) is { }
                        channel))
                        return CommandSuccessLocalized("guild_greeting_nochannel");

                    return CommandSuccessLocalized("guild_greeting_channel", args: channel.Format());
                }

                [Command("channel")]
                public async ValueTask<AdminCommandResult> SetChannelAsync(CachedTextChannel newChannel)
                {
                    if (!(await Context.Database.LoggingChannels.FindAsync(Context.Guild.Id.RawValue, LogType.Greeting)
                        is { } channel))
                    {
                        Context.Database.LoggingChannels.Add(new LoggingChannel(newChannel.Id, Context.Guild.Id,
                            LogType.Greeting));
                    }
                    else
                    {
                        channel.Id = newChannel.Id;
                        Context.Database.LoggingChannels.Update(channel);
                    }

                    await Context.Database.SaveChangesAsync();
                    return CommandSuccessLocalized("guild_greeting_channel_updated", args: newChannel.Format());
                }

                [Command("duration")]
                public async ValueTask<AdminCommandResult> GetDurationAsync()
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    return guild.GreetingDuration.HasValue
                        ? CommandSuccessLocalized("guild_greeting_duration",
                            args: guild.GreetingDuration.Value.HumanizeFormatted(Localization, Context.Language,
                                TimeUnit.Second))
                        : CommandSuccessLocalized("guild_greeting_duration_none");
                }

                [Command("duration")]
                public async ValueTask<AdminCommandResult> SetDurationAsync(TimeSpan? duration)
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    guild.GreetingDuration = duration;

                    Context.Database.Guilds.Update(guild);
                    await Context.Database.SaveChangesAsync();

                    return guild.GreetingDuration.HasValue
                        ? CommandSuccessLocalized("guild_greeting_duration_updated",
                            args: guild.GreetingDuration.Value.HumanizeFormatted(Localization, Context.Language,
                                TimeUnit.Second))
                        : CommandSuccessLocalized("guild_greeting_duration_disabled");
                }

                [Command("dm")]
                public async ValueTask<AdminCommandResult> ToggleDmAsync()
                {
                    // TODO: If toggled on, disable the greeting channel.

                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    guild.DmGreeting = !guild.DmGreeting;
                    Context.Database.Guilds.Update(guild);

                    if (guild.DmGreeting &&
                        await Context.Database.LoggingChannels.FindAsync(Context.Guild.Id.RawValue, LogType.Greeting) is
                            { } channel)
                    {
                        Context.Database.LoggingChannels.Remove(channel);
                    }

                    await Context.Database.SaveChangesAsync();

                    return guild.DmGreeting
                        ? CommandSuccessLocalized("guild_greeting_dm_enabled") // will now be DMed.
                        : CommandSuccessLocalized("guild_greeting_dm_disabled"); // will no longer be DMed.
                }
            }

            [Group("goodbye")]
            public sealed class GoodbyeCommands : GuildManagementCommands
            {
                [Command]
                public async ValueTask<AdminCommandResult> TriggerAsync()
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    if (string.IsNullOrWhiteSpace(guild.Goodbye))
                        return CommandErrorLocalized("guild_goodbye_empty");

                    var channel = await Context.Database.GetLoggingChannelAsync(Context.Guild.Id,
                        LogType.Goodbye);

                    var goodbye = await guild.Goodbye.FormatPlaceHoldersAsync(
                        AdminCommandContext.MockContext(Context.Language, Provider, Context.User, Context.Guild), Context.User.Mention, Random);

                    if (channel is null)
                        return CommandErrorLocalized("guild_goodbye_nochannel");

                    IUserMessage message;
                    if (JsonEmbed.TryParse(goodbye, out var embed))
                    {
                        message = await channel.SendMessageAsync(embed.Text ?? string.Empty, embed: embed.ToLocalEmbed());
                    }
                    else
                    {
                        message = await channel.SendMessageAsync(goodbye);
                    }

                    if (guild.GoodbyeDuration.HasValue)
                    {
                        _ = Task.Run(async () =>
                            await Task.Delay(guild.GoodbyeDuration.Value)
                                .ContinueWith(_ => message.DeleteAsync()));
                    }

                    await Context.Message.AddReactionAsync(EmojiTools.Checkmark);
                    return CommandSuccess();
                }

                [Command]
                public async ValueTask<AdminCommandResult> SetTextAsync([Remainder] string text)
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    guild.Goodbye = text;

                    Context.Database.Guilds.Update(guild);
                    await Context.Database.SaveChangesAsync();

                    return text is null // see DisableTextAsync()
                        ? CommandSuccessLocalized("guild_goodbye_disabled")
                        : CommandSuccessLocalized("guild_goodbye_updated");
                }

                [Command("disable")]
                public ValueTask<AdminCommandResult> DisableTextAsync()
                    => SetTextAsync(null);

                [Command("channel")]
                public async ValueTask<AdminCommandResult> GetChannelAsync()
                {
                    if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.Goodbye) is { }
                        channel))
                        return CommandSuccessLocalized("guild_goodbye_nochannel");

                    return CommandSuccessLocalized("guild_goodbye_channel", args: channel.Format());
                }

                [Command("channel")]
                public async ValueTask<AdminCommandResult> SetChannelAsync(CachedTextChannel newChannel)
                {
                    if (!(await Context.Database.LoggingChannels.FindAsync(Context.Guild.Id.RawValue, LogType.Goodbye)
                        is { } channel))
                    {
                        Context.Database.LoggingChannels.Add(new LoggingChannel(newChannel.Id, Context.Guild.Id,
                            LogType.Goodbye));
                    }
                    else
                    {
                        channel.Id = newChannel.Id;
                        Context.Database.LoggingChannels.Update(channel);
                    }

                    await Context.Database.SaveChangesAsync();
                    return CommandSuccessLocalized("guild_goodbye_channel_updated", args: newChannel.Format());
                }

                [Command("duration")]
                public async ValueTask<AdminCommandResult> GetDurationAsync()
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    return guild.GoodbyeDuration.HasValue
                        ? CommandSuccessLocalized("guild_goodbye_duration",
                            args: guild.GoodbyeDuration.Value.HumanizeFormatted(Localization, Context.Language,
                                TimeUnit.Second))
                        : CommandSuccessLocalized("guild_goodbye_duration_none");
                }

                [Command("duration")]
                public async ValueTask<AdminCommandResult> SetDurationAsync(TimeSpan? duration)
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    guild.GoodbyeDuration = duration;

                    Context.Database.Guilds.Update(guild);
                    await Context.Database.SaveChangesAsync();

                    return guild.GoodbyeDuration.HasValue
                        ? CommandSuccessLocalized("guild_goodbye_duration_updated",
                            args: guild.GoodbyeDuration.Value.HumanizeFormatted(Localization, Context.Language,
                                TimeUnit.Second))
                        : CommandSuccessLocalized("guild_goodbye_duration_disabled");
                }
            }
        }

        [Command("", "info")]
        [IgnoresExtraArguments]
        public AdminCommandResult GetGuildInfo()
            => CommandSuccess(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("guild_info_title", Context.Guild.Name.Sanitize()))
                .WithThumbnailUrl(Context.Guild.GetIconUrl())
                .AddField(Localize("info_id"), Context.Guild.Id)
                .AddField(Localize("info_created"),
                    string.Join('\n', Context.Guild.Id.CreatedAt.ToString("G", Context.Language.Culture),
                        (DateTimeOffset.UtcNow - Context.Guild.Id.CreatedAt).HumanizeFormatted(Localization,
                            Context.Language, TimeUnit.Second, true)))
                .AddField(Localize("info_stats_presence"),
                    Localize("guild_info_presence", Context.Guild.MemberCount,
                        Context.Guild.Members.Values.Count(x => x.IsBot), Context.Guild.TextChannels.Count,
                        Context.Guild.VoiceChannels.Count, Context.Guild.CategoryChannels.Count))
                .WithFooter(Localize("guild_info_owner", Context.Guild.Owner.Tag), Context.Guild.Owner.GetAvatarUrl())
                .Build());
    }
}