using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using FluentScheduler;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeUnit = Humanizer.Localisation.TimeUnit;

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
            BannedUserIds = new List<ulong>();
            KickedUserIds = new List<ulong>();
            MutedUserIds = new List<ulong>();

            var registry = _provider.GetRequiredService<Registry>();
            registry.Schedule(async () => await HandleExpiredPunishmentsAsync())
                .NonReentrant()
                .ToRunEvery(10)
                .Seconds();
        }

        public ICollection<ulong> BannedUserIds { get; }

        public ICollection<ulong> KickedUserIds { get; }

        public ICollection<ulong> MutedUserIds { get; }

        public async Task LogBanAsync(IUser target, SocketGuild guild, Ban ban)
        {
            using var ctx = new AdminDatabaseContext(_provider);

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
            else
            {
                moderator = await _client.GetOrDownloadUserAsync(ban.ModeratorId);
            }

            if (await ctx.GetLoggingChannelAsync(guild.Id, LogType.Ban) is SocketTextChannel logChannel)
            {
                var message =
                    await SendLoggingEmbedAsync(ban, target, moderator, null, logChannel, guildConfig.Language);
                ban.SetLogMessage(message);
                ctx.Punishments.Update(ban);
                await ctx.SaveChangesAsync();
            }

            await SendTargetEmbedAsync(ban, target, null, (await ctx.GetOrCreateGlobalUserAsync(target.Id)).Language);
        }

        public async Task LogKickAsync(IUser target, SocketGuild guild, Kick kick)
        {
            using var ctx = new AdminDatabaseContext(_provider);

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
            else
            {
                moderator = await _client.GetOrDownloadUserAsync(kick.ModeratorId);
            }

            if (await ctx.GetLoggingChannelAsync(guild.Id, LogType.Kick) is SocketTextChannel logChannel)
            {
                var message =
                    await SendLoggingEmbedAsync(kick, target, moderator, null, logChannel, guildConfig.Language);
                kick.SetLogMessage(message);
                ctx.Punishments.Update(kick);
                await ctx.SaveChangesAsync();
            }

            await SendTargetEmbedAsync(kick, target, null, (await ctx.GetOrCreateGlobalUserAsync(target.Id)).Language);
        }

        public async Task LogMuteAsync(IUser target, SocketGuild guild, IUser moderator, Mute mute)
        {
            using var ctx = new AdminDatabaseContext(_provider);

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

        public async Task LogWarningAsync(IUser target, SocketGuild guild, IUser moderator, Warning warning)
        {
            using var ctx = new AdminDatabaseContext(_provider);

            var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
            if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;

            if (await ctx.GetLoggingChannelAsync(guild.Id, LogType.Warn) is SocketTextChannel logChannel)
            {
                var message =
                    await SendLoggingEmbedAsync(warning, target, moderator, null, logChannel, guildConfig.Language);
                warning.SetLogMessage(message);
                ctx.Punishments.Update(warning);
                await ctx.SaveChangesAsync();
            }

            await SendTargetEmbedAsync(warning, target, null, (await ctx.GetOrCreateGlobalUserAsync(target.Id)).Language);
        }

        public async Task LogAppealAsync(IUser target, SocketGuild guild, RevocablePunishment punishment)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            
            var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
            if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;
            if (!(await ctx.GetLoggingChannelAsync(guild.Id, LogType.Appeal) is SocketTextChannel logChannel))
                return;

            await logChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithSuccessColor()
                .WithTitle(_localization.Localize(guildConfig.Language,
                               $"punishment_{punishment.GetType().Name.ToLower()}") +
                           $" - {_localization.Localize(guildConfig.Language, "punishment_case", punishment.Id)}")
                .WithDescription(_localization.Localize(guildConfig.Language, "punishment_appeal_description_guild",
                    target.Format()))
                .AddField("title_reason", punishment.AppealReason)
                .WithTimestamp(punishment.AppealedAt.Value).Build());
        }

        private async Task<IUserMessage> SendLoggingEmbedAsync(Punishment punishment, IUser target, IUser moderator, Punishment additionalPunishment, SocketTextChannel logChannel, LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new EmbedBuilder()
                .WithErrorColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .AddField(_localization.Localize(language, "title_reason"),
                    punishment.Reason ?? _localization.Localize(language, "punishment_needsreason",
                        Format.Code(
                            $"{_config.DefaultPrefix}reason {punishment.Id} [{_localization.Localize(language, "title_reason").ToLower()}]")))
                .WithFooter(_localization.Localize(language, "punishment_moderator", moderator.ToString()),
                    moderator.GetAvatarOrDefault())
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

            if (punishment is Mute channelMute && channelMute.ChannelId.HasValue)
            {
                builder.AddField(_localization.Localize(language, "punishment_mute_channel"),
                    _client.GetGuild(punishment.GuildId).GetTextChannel(channelMute.ChannelId.Value).Mention);
            }

            if (punishment is Warning)
            {
                using var ctx = new AdminDatabaseContext(_provider);

                var warningCount = await ctx.Punishments.OfType<Warning>().CountAsync(x =>
                    x.TargetId == target.Id && x.GuildId == punishment.GuildId && !x.RevokedAt.HasValue);
                builder.WithDescription(_localization.Localize(language, "punishment_warning_description_guild", $"**{target}** (`{target.Id}`)",
                    Format.Bold(warningCount.ToOrdinalWords(language.Culture))));

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

        public async Task SendLoggingRevocationEmbedAsync(RevocablePunishment punishment, IUser target, IUser moderator,
            SocketTextChannel logChannel, LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new EmbedBuilder().WithWarnColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .WithDescription(_localization.Localize(language, $"punishment_{typeName}_revoke_description_guild",
                    $"**{target}** (`{target.Id}`)"))
                .AddField(_localization.Localize(language, "title_reason"),
                    punishment.RevocationReason ?? _localization.Localize(language, "punishment_noreason"))
                .WithFooter(_localization.Localize(language, "punishment_moderator", moderator.ToString()),
                    moderator.GetAvatarOrDefault())
                .WithTimestamp(punishment.RevokedAt ?? DateTimeOffset.UtcNow);

            if (punishment is Mute channelMute && channelMute.ChannelId.HasValue)
            {
                builder.AddField(_localization.Localize(language, "punishment_mute_channel"),
                    _client.GetGuild(punishment.GuildId).GetTextChannel(channelMute.ChannelId.Value)?.Mention ?? "???");
            }

            await logChannel.SendMessageAsync(embed: builder.Build());
        }

        private async Task SendTargetEmbedAsync(Punishment punishment, IUser target, Punishment additionalPunishment, LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new EmbedBuilder().WithErrorColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .AddField(_localization.Localize(language, "title_reason"),
                    punishment.Reason ?? _localization.Localize(language, "punishment_noreason"))
                .WithTimestamp(punishment.CreatedAt);

            if (!(additionalPunishment is null))
            {
                builder.AddField(FormatAdditionalPunishment(additionalPunishment, language));
            }

            if (punishment is Warning)
            {
                using var ctx = new AdminDatabaseContext(_provider);

                var warningCount = await ctx.Punishments.OfType<Warning>().CountAsync(x =>
                    x.TargetId == target.Id && x.GuildId == punishment.GuildId && !x.RevokedAt.HasValue);
                builder.WithDescription(_localization.Localize(language, "punishment_warning_description", _client.GetGuild(punishment.GuildId).Name,
                    Format.Bold(warningCount.ToOrdinalWords(language.Culture))));
            }
            else
            {
                builder.WithDescription(_localization.Localize(language, $"punishment_{typeName}_description",
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
                        Format.Code($"{_config.DefaultPrefix}appeal {punishment.Id} [{_localization.Localize(language, "title_reason").ToLower()}]"));
                }
            }

            if (punishment is Mute channelMute && channelMute.ChannelId.HasValue)
            {
                builder.AddField(_localization.Localize(language, "punishment_mute_channel"),
                    _client.GetGuild(punishment.GuildId).GetTextChannel(channelMute.ChannelId.Value).Mention);
            }

            _ = target.SendMessageAsync(embed: builder.Build());
        }

        public Task SendTargetRevocationEmbedAsync(RevocablePunishment punishment, IUser target,
            LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new EmbedBuilder().WithWarnColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .WithDescription(_localization.Localize(language, $"punishment_{typeName}_revoke_description", _client.GetGuild(punishment.GuildId)?.Name))
                .AddField(_localization.Localize(language, "title_reason"),
                    punishment.RevocationReason ?? _localization.Localize(language, "punishment_noreason"))
                .WithTimestamp(punishment.RevokedAt ?? DateTimeOffset.UtcNow);

            if (punishment is Mute channelMute && channelMute.ChannelId.HasValue)
            {
                builder.AddField(_localization.Localize(language, "punishment_mute_channel"),
                    _client.GetGuild(punishment.GuildId).GetTextChannel(channelMute.ChannelId.Value).Mention);
            }

            return target.SendMessageAsync(embed: builder.Build());
        }

        public EmbedFieldBuilder FormatAdditionalPunishment(Punishment punishment, LocalizedLanguage language)
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

        public async Task HandleMuteEvasionAsync(SocketGuildUser user)
        {
            await using var ctx = new AdminDatabaseContext(_provider);

            if (await ctx.Punishments.OfType<Mute>()
                .FirstOrDefaultAsync(x => x.GuildId == user.Guild.Id && x.TargetId == user.Id && !x.IsExpired) is { } mute)
            {
                if (mute.ChannelId.HasValue)
                {
                    var channel = user.Guild.GetTextChannel(mute.ChannelId.Value);
                    await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny));
                    return;
                }

                if (await ctx.GetSpecialRoleAsync(user.Guild.Id, RoleType.Mute) is { } muteRole)
                {
                    await user.AddRoleAsync(muteRole);
                }
            }
        }

        private async Task HandleExpiredPunishmentsAsync()
        {
            using var ctx = new AdminDatabaseContext(_provider);

            var expiredMutes = await ctx.Punishments.OfType<Mute>().ToListAsync();
            expiredMutes = expiredMutes
                .Where(x => x.IsExpired && !x.RevokedAt.HasValue).ToList();
            var expiredBans = await ctx.Punishments.OfType<Ban>().ToListAsync();
            expiredBans = expiredBans
                .Where(x => x.IsExpired && !x.RevokedAt.HasValue).ToList();


            foreach (var mute in expiredMutes)
            {
                var guild = await ctx.GetOrCreateGuildAsync(mute.GuildId);
                mute.Revoke(_client.CurrentUser.Id, _localization.Localize(guild.Language, "punishment_mute_expired"));
                ctx.Punishments.Update(mute);

                var target = await _client.GetOrDownloadUserAsync(mute.TargetId);
                if (await ctx.GetLoggingChannelAsync(mute.GuildId, LogType.Unmute) is SocketTextChannel logChannel)
                {
                    await SendLoggingRevocationEmbedAsync(mute, target, _client.CurrentUser, logChannel,
                        guild.Language);
                }

                var user = await ctx.GetOrCreateGlobalUserAsync(mute.TargetId);
                _ = SendTargetRevocationEmbedAsync(mute, target, user.Language);

                if (mute.ChannelId.HasValue &&
                    _client.GetChannel(mute.ChannelId.Value) is SocketTextChannel muteChannel)
                {
                    _ = muteChannel.RemovePermissionOverwriteAsync(target);
                }
                else if (_client.GetGuild(mute.GuildId).GetUser(mute.TargetId) is SocketGuildUser guildUser &&
                         await ctx.GetSpecialRoleAsync(mute.GuildId, RoleType.Mute) is SocketRole muteRole)
                {
                    _ = guildUser.RemoveRoleAsync(muteRole);
                }
            }

            foreach (var ban in expiredBans)
            {
                var guild = await ctx.GetOrCreateGuildAsync(ban.GuildId);
                ban.Revoke(_client.CurrentUser.Id, _localization.Localize(guild.Language, "punishment_mute_expired"));
                ctx.Punishments.Update(ban);

                var target = await _client.GetOrDownloadUserAsync(ban.TargetId);
                if (await ctx.GetLoggingChannelAsync(ban.GuildId, LogType.Unban) is SocketTextChannel logChannel)
                {
                    await SendLoggingRevocationEmbedAsync(ban, target, _client.CurrentUser, logChannel,
                        guild.Language);
                }

                var user = await ctx.GetOrCreateGlobalUserAsync(ban.TargetId);
                _ = SendTargetRevocationEmbedAsync(ban, target, user.Language);

                _ = _client.GetGuild(ban.GuildId).RemoveBanAsync(ban.TargetId);
            }

            await ctx.SaveChangesAsync();
        }

        Task IService.InitializeAsync()
            => _logging.LogInfoAsync("Initialized.", "Punishments");
    }
}