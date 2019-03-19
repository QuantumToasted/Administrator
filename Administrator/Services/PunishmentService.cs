using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Services
{
    public sealed class PunishmentService : IService
    {
        private readonly DiscordSocketClient _client;
        private readonly LoggingService _logging;
        private readonly LocalizationService _localization;
        private readonly ConfigurationService _config;

        public PunishmentService(DiscordSocketClient client, LoggingService logging, LocalizationService localization,
            ConfigurationService config)
        {
            _client = client;
            _logging = logging;
            _localization = localization;
            _config = config;
        }

        Task IService.InitializeAsync()
            => _logging.LogInfoAsync("Initialized.", "Punishments");

        public async Task LogBanAsync(SocketGuildUser target, Ban ban)
        {
            using (var ctx = new AdminDatabaseContext())
            {
                if (!(await ctx.GetLoggingChannelAsync(target.Guild.Id, LogType.Ban) is SocketTextChannel logChannel) ||
                    !target.Guild.CurrentUser.GetPermissions(logChannel).SendMessages)
                    return;

                IUser moderator = null;
                string reason = null;
                if (ban is null)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        var entries = await target.Guild.GetAuditLogsAsync(10).FlattenAsync();

                        foreach (var entry in entries.OrderByDescending(x => x.Id))
                        {
                            if (entry.Action == ActionType.Ban &&
                                (entry.Data as BanAuditLogData)?.Target.Id == target.Id)
                            {
                                moderator = entry.User;
                                reason = entry.Reason;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    ban = new Ban(target.Guild.Id, target.Id, moderator?.Id ?? target.Guild.CurrentUser.Id, reason);
                    ctx.Punishments.Add(ban);
                    await ctx.SaveChangesAsync();
                }

                var guild = await ctx.GetOrCreateGuildAsync(target.Guild.Id);
                var logEmbed = await FormatLoggingEmbedAsync(ban, target, moderator ?? target.Guild.CurrentUser,
                    guild.Language, null);
                var dmEmbed = await FormatTargetEmbedAsync(ban, target, guild.Language, null);

                _ = target.SendMessageAsync(embed: dmEmbed);
                await target.BanAsync(7, FormatAuditLogReason(ban, moderator, guild.Language));

                var logMessage = await logChannel.SendMessageAsync(embed: logEmbed);
                ban.SetLogMessage(logMessage);
                ctx.Update(ban);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task LogKickAsync(SocketGuildUser target, Kick kick)
        {
            using (var ctx = new AdminDatabaseContext())
            {
                if (!(await ctx.GetLoggingChannelAsync(target.Guild.Id,
                        LogType.Kick) is SocketTextChannel logChannel) ||
                    !target.Guild.CurrentUser.GetPermissions(logChannel).SendMessages)
                    return;

                IUser moderator = null;
                string reason = null;
                if (kick is null)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        var entries = await target.Guild.GetAuditLogsAsync(10).FlattenAsync();


                        foreach (var entry in entries.OrderByDescending(x => x.Id))
                        {
                            if (entry.Action == ActionType.Kick &&
                                (entry.Data as KickAuditLogData)?.Target.Id == target.Id)
                            {
                                moderator = entry.User;
                                reason = entry.Reason;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    kick = new Kick(target.Guild.Id, target.Id, moderator?.Id ?? target.Guild.CurrentUser.Id, reason);
                    ctx.Punishments.Add(kick);
                    await ctx.SaveChangesAsync();
                }

                var guild = await ctx.GetOrCreateGuildAsync(target.Guild.Id);
                var logEmbed = await FormatLoggingEmbedAsync(kick, target, moderator ?? target.Guild.CurrentUser,
                    guild.Language, null);
                var dmEmbed = await FormatTargetEmbedAsync(kick, target, guild.Language, null);

                _ = target.SendMessageAsync(embed: dmEmbed);
                await target.KickAsync(FormatAuditLogReason(kick, moderator, guild.Language));

                var logMessage = await logChannel.SendMessageAsync(embed: logEmbed);
                kick.SetLogMessage(logMessage);
                ctx.Update(kick);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task LogMuteAsync(SocketGuildUser target, Mute mute)
        {
            using (var ctx = new AdminDatabaseContext())
            {
                if (!(await ctx.GetLoggingChannelAsync(target.Guild.Id, LogType.Mute) is SocketTextChannel logChannel) ||
                    !target.Guild.CurrentUser.GetPermissions(logChannel).SendMessages)
                    return;

                if (!(await ctx.GetSpecialRoleAsync(target.Guild.Id, RoleType.Mute) is SocketRole muteRole))
                    return;

                IUser moderator = null;
                string reason = null;
                if (mute is null)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        var entries = await target.Guild.GetAuditLogsAsync(10).FlattenAsync();

                        foreach (var entry in entries.OrderByDescending(x => x.Id))
                        {
                            if (entry.Action == ActionType.MemberRoleUpdated &&
                                (entry.Data as MemberRoleAuditLogData)?.Target.Id == target.Id &&
                                (entry.Data as MemberRoleAuditLogData)?.Roles.Any(x => x.Added && x.RoleId == muteRole.Id) == true)
                            {
                                moderator = entry.User;
                                reason = entry.Reason;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    mute = new Mute(target.Guild.Id, target.Id, moderator?.Id ?? target.Guild.CurrentUser.Id, reason, null);
                    ctx.Punishments.Add(mute);
                    await ctx.SaveChangesAsync();
                }
                else
                {
                    await target.AddRoleAsync(muteRole);
                }

                var guild = await ctx.GetOrCreateGuildAsync(target.Guild.Id);
                var logEmbed = await FormatLoggingEmbedAsync(mute, target, moderator ?? target.Guild.CurrentUser,
                    guild.Language, null);
                var dmEmbed = await FormatTargetEmbedAsync(mute, target, guild.Language, null);

                _ = target.SendMessageAsync(embed: dmEmbed);

                var logMessage = await logChannel.SendMessageAsync(embed: logEmbed);
                mute.SetLogMessage(logMessage);
                ctx.Update(mute);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task LogWarningAsync(SocketGuildUser target, Warning warning)
        {
            using (var ctx = new AdminDatabaseContext())
            {
                if (!(await ctx.GetLoggingChannelAsync(target.Guild.Id,
                        LogType.Warn) is SocketTextChannel logChannel) ||
                    !target.Guild.CurrentUser.GetPermissions(logChannel).SendMessages)
                    return;

                var moderator = target.Guild.GetUser(warning.ModeratorId) ?? target.Guild.CurrentUser;

                var guild = await ctx.GetOrCreateGuildAsync(target.Guild.Id);
                var logEmbed = await FormatLoggingEmbedAsync(warning, target, moderator, guild.Language, null);
                var dmEmbed = await FormatTargetEmbedAsync(warning, target, guild.Language, null);

                _ = target.SendMessageAsync(embed: dmEmbed);

                var logMessage = await logChannel.SendMessageAsync(embed: logEmbed);
                warning.SetLogMessage(logMessage);
                ctx.Update(warning);
                await ctx.SaveChangesAsync();
            }
        }

        // Must be the guild's language!
        private async Task<Embed> FormatLoggingEmbedAsync(Punishment punishment, SocketGuildUser target, IUser moderator, LocalizedLanguage language, Punishment additionalPunishment)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new EmbedBuilder()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .AddField(_localization.Localize(language, "punishment_reason"),
                    punishment.Reason ?? _localization.Localize(language, "punishment_needsreason",
                        Format.Code(
                            $"{_config.DefaultPrefix}reason {punishment.Id} [{_localization.Localize(language, "punishment_reason").ToLower()}]")))
                .WithFooter(_localization.Localize(language, "punishment_moderator", moderator.ToString()),
                    moderator.GetAvatarUrl() ?? moderator.GetDefaultAvatarUrl());

            if (punishment is Warning)
            {
                using (var ctx = new AdminDatabaseContext())
                {
                    var warningCount = await ctx.Punishments.OfType<Warning>().CountAsync(x =>
                        x.TargetId == target.Id && x.GuildId == target.Guild.Id && !x.IsRevoked);
                    builder.WithDescription(_localization.Localize(language, "punishment_warning_description_guild", $"**{target}** (`{target.Id}`)",
                        Format.Bold(warningCount.ToOrdinalWords(language.Culture))));
                }

                if (!(additionalPunishment is null))
                {
                    builder.AddField(FormatAdditionalPunishment(additionalPunishment, language));
                }

                builder.WithWarnColor();
            }
            else
            {
                builder.WithDescription(_localization.Localize(language, $"punishnent_{typeName}_description_guild",
                        $"**{target}** (`{target.Id}`)"))
                    .WithErrorColor();
            }

            return builder.Build();
        }

        // Must be the target's language!
        private async Task<Embed> FormatTargetEmbedAsync(Punishment punishment, SocketGuildUser target, LocalizedLanguage language, Punishment additionalPunishment)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new EmbedBuilder().WithErrorColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .AddField(_localization.Localize(language, "punishment_reason"),
                    punishment.Reason ?? _localization.Localize(language, "punishment_noreason"))
                .WithTimestamp(punishment.CreatedAt);

            if (!(additionalPunishment is null))
            {
                builder.AddField(FormatAdditionalPunishment(additionalPunishment, language));
            }

            if (punishment is Warning)
            {
                using (var ctx = new AdminDatabaseContext())
                {
                    var warningCount = await ctx.Punishments.OfType<Warning>().CountAsync(x =>
                        x.TargetId == target.Id && x.GuildId == target.Guild.Id && !x.IsRevoked);
                    builder.WithDescription(_localization.Localize(language, "punishment_warning_description",
                        Format.Bold(warningCount.ToOrdinalWords(language.Culture))));
                }
            }
            else
            {
                builder.WithDescription(_localization.Localize(language, $"punishnent_{typeName}_description",
                    target.Guild.Name));
            }

            if (punishment is RevocablePunishment revocable)
            {
                var field = new EmbedFieldBuilder()
                    .WithName(_localization.Localize(language, "punishment_appeal"));

                switch (revocable)
                {
                    case Ban _:
                        field.WithValue(GetAppealInstructions());
                        break;
                    case Mute mute:
                        field.WithValue(!mute.Duration.HasValue || mute.Duration.Value > TimeSpan.FromDays(1)
                            ? GetAppealInstructions()
                            : _localization.Localize(language, "punishment_tooshort",
                                mute.Duration.Value.Humanize(minUnit: TimeUnit.Minute, culture: language.Culture)));
                        break;
                    case TemporaryBan tempBan:
                        field.WithValue(tempBan.Duration > TimeSpan.FromDays(1)
                            ? GetAppealInstructions()
                            : _localization.Localize(language, "punishment_tooshort",
                                tempBan.Duration.Humanize(minUnit: TimeUnit.Minute, culture: language.Culture)));
                        break;
                    case Warning _:
                        field = null;
                        break;
                }

                if (!(field is null))
                    builder.AddField(field);

                string GetAppealInstructions()
                {
                    return _localization.Localize(language, "punishment_appeal_instructions", 
                        Format.Code(punishment.Id.ToString()), 
                        Format.Code($"{_config.DefaultPrefix}appeal {punishment.Id} [{_localization.Localize(language, "punishment_reason").ToLower()}]"));
                }
            }

            return builder.Build();
        }

        private EmbedFieldBuilder FormatAdditionalPunishment(Punishment punishment, LocalizedLanguage language)
        {
            var field = new EmbedFieldBuilder()
                .WithName(_localization.Localize(language, "punishment_warning_additional"));
            switch (punishment)
            {
                case Kick _:
                    field.WithValue(_localization.Localize(language, "punishment_kick") + $" (#{punishment.Id}) ");
                    break;
                case Ban _:
                    field.WithValue(_localization.Localize(language, "punishment_ban") + $" (#{punishment.Id}) ");
                    break;
                case Mute mute:
                    field.WithValue(_localization.Localize(language, "punishment_mute") + $" (#{punishment.Id}) " +
                                    $" ({(mute.Duration.HasValue ? mute.Duration.Value.Humanize(minUnit: TimeUnit.Second, culture: language.Culture) : _localization.Localize(language, "punishment_mute_permanent"))})");
                    break;
                case TemporaryBan tempBan:
                    field.WithValue(_localization.Localize(language, "punishment_temporaryban") + $" (#{punishment.Id}) " +
                                    $" ({tempBan.Duration.Humanize(minUnit: TimeUnit.Second, culture: language.Culture)})");
                    break;
            }

            return field;
        }

        private string FormatAuditLogReason(Punishment punishment, IUser moderator, LocalizedLanguage language)
            => new StringBuilder(punishment.Reason ?? _localization.Localize(language, "punishment_noreason"))
                .Append($" | {_localization.Localize(language, "punishment_moderator", moderator.ToString())}")
                .Append($" | {punishment.CreatedAt.ToString("g", language.Culture)} UTC")
                .ToString();
    }
}