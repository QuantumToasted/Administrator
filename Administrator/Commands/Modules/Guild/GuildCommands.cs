using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    [Name("Guild")]
    [Group("guild", "server")]
    [RequireContext(ContextType.Guild)]
    public class GuildCommands : AdminModuleBase
    {
        [RequireUserPermissions(Permission.ManageGuild)]
        public class ManageGuildCommands : GuildCommands
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

                return CommandSuccessLocalized("guild_language_set", args:
                    $"{Markdown.Bold(guild.Language.NativeName)} ({guild.Language.EnglishName}, `{guild.Language.CultureCode}`)");
            }

            [Command("languages")]
            public AdminCommandResult GetLanguages()
                => CommandSuccess(new StringBuilder()
                    .AppendLine(Localize("available_languages"))
                    .AppendJoin('\n',
                        Localization.Languages.Select(
                            x => Markdown.Code($"{x.NativeName} ({x.EnglishName}, {x.CultureCode})"))).ToString());

            [Group("settings")]
            public class GuildSettingsCommands : ManageGuildCommands
            {
                [Command("", "list")]
                public async ValueTask<AdminCommandResult> ListGuildSettingsAsync()
                {
                    var builder =
                        new StringBuilder(Localize("guild_settings", Markdown.Bold(Context.Guild.Name.Sanitize())))
                            .AppendLine()
                            .AppendLine();

                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    foreach (var value in Enum.GetValues(typeof(GuildSettings)).Cast<GuildSettings>()
                        .Where(x => !x.Equals(default)))
                    {
                        var enabled = guild.Settings.HasFlag(value);
                        builder.Append($"`{value:G}` - ")
                            .AppendLine(Localize($"info_{(enabled ? "enabled" : "disabled")}"));
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
            public class GuildPrefixCommands : ManageGuildCommands
            {
                [Command("", "list")]
                public async ValueTask<AdminCommandResult> ListPrefixesAsync()
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    if (guild.CustomPrefixes.Count == 0)
                        return CommandErrorLocalized("guild_prefixes_none");

                    return CommandSuccess(
                        new StringBuilder(Localize("guild_prefixes", Markdown.Bold(Context.Guild.Name.Sanitize())))
                            .AppendLine()
                            .AppendLine()
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
                        Markdown.Bold(guild.XpGainInterval.HumanizeFormatted(Context))
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
                    Markdown.Bold(guild.XpGainInterval.HumanizeFormatted(Context))
                });
            }

            [Command("levelemoji")]
            public async ValueTask<AdminCommandResult> GetLevelEmojiAsync()
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                return CommandSuccessLocalized("guild_levelemoji", args: guild.LevelUpEmoji);
            }

            [Command("levelemoji")]
            public async ValueTask<AdminCommandResult> SetLevelEmojiAsync(IEmoji emoji)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                guild.LevelUpEmoji = emoji;
                Context.Database.Guilds.Update(guild);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("guild_levelemoji_set", args: emoji);
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
                            .AppendLine()
                            .AppendLine();

                    foreach (var value in Enum.GetValues(typeof(LevelUpNotification)).Cast<LevelUpNotification>()
                        .Where(x => !x.Equals(LevelUpNotification.None)))
                    {
                        var enabled = guild.LevelUpWhitelist.HasFlag(value);
                        builder.Append($"`{value:G}` - ")
                            .AppendLine(Localize($"info_{(enabled ? "enabled" : "disabled")}"));
                    }

                    return builder.ToString();
                }
            }
        }
    }
}