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
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Services
{
    public sealed class PunishmentService : IService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly LoggingService _logging;
        private readonly LocalizationService _localization;
        private readonly ConfigurationService _config;

        public PunishmentService(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetRequiredService<DiscordSocketClient>();
            _logging = _provider.GetRequiredService<LoggingService>();
            _localization = _provider.GetRequiredService<LocalizationService>();
            _config = _provider.GetRequiredService<ConfigurationService>();
        }

        public async Task LogBanAsync(SocketUser target, SocketGuild guild, Ban ban)
        {
            using (var ctx = new AdminDatabaseContext(_provider))
            {
                var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
                if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;

                var moderator = guild.CurrentUser as IUser;
                if (ban is null)
                {
                    string reason = null;
                    if (guild.CurrentUser.GuildPermissions.ViewAuditLog)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        var entries = await guild.GetAuditLogsAsync(15).FlattenAsync();
                        if (entries.OrderByDescending(x => x.Id)
                                .FirstOrDefault(x => x.Data is BanAuditLogData data && data.Target.Id == target.Id) is
                            RestAuditLogEntry entry)
                        {
                            moderator = entry.User;
                            reason = entry.Reason;
                        }
                    }

                    ban = new Ban(guild.Id, target.Id, moderator.Id, reason, null);
                    ctx.Punishments.Add(ban);
                    await ctx.SaveChangesAsync();
                }

                if (!(await ctx.GetLoggingChannelAsync(guild.Id, LogType.Ban) is SocketTextChannel logChannel))
                    return;

                var message =
                    await SendLoggingEmbedAsync(ban, target, moderator, null, logChannel, guildConfig.Language);
                ban.SetLogMessage(message);
                ctx.Punishments.Update(ban);
                await ctx.SaveChangesAsync();

                await SendTargetEmbedAsync(ban, target, null, (await ctx.GetOrCreateGlobalUserAsync(target.Id)).Language);
            }
        }

        public async Task LogKickAsync(SocketUser target, SocketGuild guild, Kick kick)
        {
            using (var ctx = new AdminDatabaseContext(_provider))
            {
                var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
                if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;

                var moderator = guild.CurrentUser as IUser;
                if (kick is null)
                {
                    string reason = null;
                    if (guild.CurrentUser.GuildPermissions.ViewAuditLog)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        var entries = await guild.GetAuditLogsAsync(15).FlattenAsync();
                        if (entries.OrderByDescending(x => x.Id)
                                .FirstOrDefault(x => x.Data is KickAuditLogData data && data.Target.Id == target.Id) is
                            RestAuditLogEntry entry)
                        {
                            moderator = entry.User;
                            reason = entry.Reason;
                        }
                    }

                    kick = new Kick(guild.Id, target.Id, moderator.Id, reason);
                    ctx.Punishments.Add(kick);
                    await ctx.SaveChangesAsync();
                }

                if (!(await ctx.GetLoggingChannelAsync(guild.Id, LogType.Kick) is SocketTextChannel logChannel))
                    return;

                var message =
                    await SendLoggingEmbedAsync(kick, target, moderator, null, logChannel, guildConfig.Language);
                kick.SetLogMessage(message);
                ctx.Punishments.Update(kick);
                await ctx.SaveChangesAsync();

                await SendTargetEmbedAsync(kick, target, null, (await ctx.GetOrCreateGlobalUserAsync(target.Id)).Language);
            }
        }

        public async Task LogMuteAsync(SocketUser target, SocketGuild guild, IUser moderator, Mute mute)
        {
            using (var ctx = new AdminDatabaseContext(_provider))
            {
                var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
                if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;

                if (!(await ctx.GetLoggingChannelAsync(guild.Id, LogType.Mute) is SocketTextChannel logChannel))
                    return;

                var message =
                    await SendLoggingEmbedAsync(mute, target, moderator, null, logChannel, guildConfig.Language);
                mute.SetLogMessage(message);
                ctx.Punishments.Update(mute);
                await ctx.SaveChangesAsync();

                await SendTargetEmbedAsync(mute, target, null, (await ctx.GetOrCreateGlobalUserAsync(target.Id)).Language);
            }
        }

        public async Task LogWarningAsync(SocketUser target, SocketGuild guild, IUser moderator, Warning warning)
        {
            using (var ctx = new AdminDatabaseContext(_provider))
            {
                var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
                if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;

                if (!(await ctx.GetLoggingChannelAsync(guild.Id, LogType.Warn) is SocketTextChannel logChannel))
                    return;

                var message =
                    await SendLoggingEmbedAsync(warning, target, moderator, null, logChannel, guildConfig.Language);
                warning.SetLogMessage(message);
                ctx.Punishments.Update(warning);
                await ctx.SaveChangesAsync();

                await SendTargetEmbedAsync(warning, target, null, (await ctx.GetOrCreateGlobalUserAsync(target.Id)).Language);
            }
        }

        private async Task<IUserMessage> SendLoggingEmbedAsync(Punishment punishment, SocketUser target, IUser moderator, Punishment additionalPunishment, SocketTextChannel logChannel, LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new EmbedBuilder()
                .WithErrorColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .AddField(_localization.Localize(language, "punishment_reason"),
                    punishment.Reason ?? _localization.Localize(language, "punishment_needsreason",
                        Format.Code(
                            $"{_config.DefaultPrefix}reason {punishment.Id} [{_localization.Localize(language, "punishment_reason").ToLower()}]")))
                .WithFooter(_localization.Localize(language, "punishment_moderator", moderator.ToString()),
                    moderator.GetAvatarUrl() ?? moderator.GetDefaultAvatarUrl())
                .WithTimestamp(punishment.CreatedAt);

            builder = punishment switch
            {
                Ban ban => builder.AddField(
                    _localization.Localize(language, "punishment_duration"), ban.Duration.HasValue
                        ? ban.Duration.Value.Humanize(minUnit: TimeUnit.Second, culture: language.Culture)
                        : _localization.Localize(language, "punishment_permanent")),
                Mute mute => builder.AddField(
                    _localization.Localize(language, "punishment_duration"), mute.Duration.HasValue
                        ? mute.Duration.Value.Humanize(minUnit: TimeUnit.Second, culture: language.Culture)
                        : _localization.Localize(language, "punishment_permanent")),
                _ => builder
            };

            if (punishment is Warning)
            {
                using (var ctx = new AdminDatabaseContext(_provider))
                {
                    var warningCount = await ctx.Punishments.OfType<Warning>().CountAsync(x =>
                        x.TargetId == target.Id && x.GuildId == punishment.GuildId && !x.IsRevoked);
                    builder.WithDescription(_localization.Localize(language, "punishment_warning_description_guild", $"**{target}** (`{target.Id}`)",
                        Format.Bold(warningCount.ToOrdinalWords(language.Culture))));
                }

                if (!(additionalPunishment is null))
                {
                    builder.AddField(FormatAdditionalPunishment(additionalPunishment, language));
                }
            }
            else
            {
                builder.WithDescription(_localization.Localize(language, $"punishment_{typeName}_description_guild",
                        $"**{target}** (`{target.Id}`)"));
            }

            return await logChannel.SendMessageAsync(embed: builder.Build());
        }

        private async Task SendLoggingRevocationEmbedAsync(RevocablePunishment punishment, SocketUser target, IUser moderator,
            SocketTextChannel logChannel, LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new EmbedBuilder().WithWarnColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .WithDescription(_localization.Localize(language, $"punishment_{typeName}_revoke_description_guild",
                    $"**{target}** (`{target.Id}`)"))
                .AddField(_localization.Localize(language, "punishment_reason"),
                    punishment.RevocationReason ?? _localization.Localize(language, "punishment_noreason"))
                .WithFooter(_localization.Localize(language, "punishment_moderator", moderator.ToString()),
                    moderator.GetAvatarUrl() ?? moderator.GetDefaultAvatarUrl())
                .WithTimestamp(punishment.RevokedAt ?? DateTimeOffset.UtcNow);

            await logChannel.SendMessageAsync(embed: builder.Build());
        }

        private async Task SendTargetEmbedAsync(Punishment punishment, SocketUser target, Punishment additionalPunishment, LocalizedLanguage language)
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
                using (var ctx = new AdminDatabaseContext(_provider))
                {
                    var warningCount = await ctx.Punishments.OfType<Warning>().CountAsync(x =>
                        x.TargetId == target.Id && x.GuildId == punishment.GuildId && !x.IsRevoked);
                    builder.WithDescription(_localization.Localize(language, "punishment_warning_description",
                        Format.Bold(warningCount.ToOrdinalWords(language.Culture))));
                }
            }
            else
            {
                builder.WithDescription(_localization.Localize(language, $"punishnent_{typeName}_description",
                    _client.GetGuild(punishment.GuildId).Name));
            }

            if (punishment is RevocablePunishment revocable)
            {
                var field = new EmbedFieldBuilder()
                    .WithName(_localization.Localize(language, "punishment_appeal"));

                switch (revocable)
                {
                    case Ban ban:
                        field.WithValue(!ban.Duration.HasValue || ban.Duration.Value > TimeSpan.FromDays(1)
                            ? GetAppealInstructions()
                            : _localization.Localize(language, "punishment_tooshort",
                                ban.Duration.Value.Humanize(minUnit: TimeUnit.Minute, culture: language.Culture)));
                        break;
                    case Mute mute:
                        field.WithValue(!mute.Duration.HasValue || mute.Duration.Value > TimeSpan.FromDays(1)
                            ? GetAppealInstructions()
                            : _localization.Localize(language, "punishment_tooshort",
                                mute.Duration.Value.Humanize(minUnit: TimeUnit.Minute, culture: language.Culture)));
                        break;
                    default:
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

            _ = target.SendMessageAsync(embed: builder.Build());
        }

        private async Task SendTargetRevocationEmbedAsync(RevocablePunishment punishment, SocketUser target,
            LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new EmbedBuilder().WithWarnColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .WithDescription(_localization.Localize(language, $"punishment_{typeName}_revoke_description"))
                .AddField(_localization.Localize(language, "punishment_reason"),
                    punishment.RevocationReason ?? _localization.Localize(language, "punishment_noreason"))
                .WithTimestamp(punishment.RevokedAt ?? DateTimeOffset.UtcNow);
            _ = target.SendMessageAsync(embed: builder.Build());
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
                case Ban ban:
                    field.WithValue(_localization.Localize(language, "punishment_ban") + $" (#{punishment.Id}) " +
                                    $" ({(ban.Duration.HasValue ? ban.Duration.Value.Humanize(minUnit: TimeUnit.Second, culture: language.Culture) : _localization.Localize(language, "punishment_permanent"))})");
                    break;
                case Mute mute:
                    field.WithValue(_localization.Localize(language, "punishment_mute") + $" (#{punishment.Id}) " +
                                    $" ({(mute.Duration.HasValue ? mute.Duration.Value.Humanize(minUnit: TimeUnit.Second, culture: language.Culture) : _localization.Localize(language, "punishment_permanent"))})");
                    break;
            }

            return field;
        }

        private string FormatAuditLogReason(Punishment punishment, IUser moderator, LocalizedLanguage language)
            => new StringBuilder(punishment.Reason ?? _localization.Localize(language, "punishment_noreason"))
                .Append($" | {_localization.Localize(language, "punishment_moderator", moderator.ToString())}")
                .Append($" | {punishment.CreatedAt.ToString("g", language.Culture)} UTC")
                .ToString();

        Task IService.InitializeAsync()
            => _logging.LogInfoAsync("Initialized.", "Punishments");
    }
}