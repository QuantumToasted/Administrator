using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.Events;
using Disqord.Rest.AuditLogs;
using FluentScheduler;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Permission = Disqord.Permission;
using TimeUnit = Humanizer.Localisation.TimeUnit;

namespace Administrator.Services
{
    public sealed class PunishmentService : IService, IHandler<MemberBannedEventArgs>,
        IHandler<MemberLeftEventArgs>, IHandler<MemberJoinedEventArgs>
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordClient _client;
        private readonly LoggingService _logging;
        private readonly LocalizationService _localization;
        private readonly ConfigurationService _config;

        public PunishmentService(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetRequiredService<DiscordClient>();
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

        public Task HandleAsync(MemberBannedEventArgs args)
            => LogBanAsync(args.User, args.Guild, null);

        public async Task LogBanAsync(IUser target, CachedGuild guild, Ban ban)
        {
            using var ctx = new AdminDatabaseContext(_provider);

            var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
            if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;

            var moderator = guild.CurrentMember as IUser;
            if (ban is null)
            {
                if (!guildConfig.Settings.HasFlag(GuildSettings.AutoPunishments) || BannedUserIds.Remove(target.Id))
                    return;

                string reason = null;
                if (guild.CurrentMember.Permissions.ViewAuditLog)
                {
                    // TODO: Adjust delay if necessary.
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    var logs = await guild.GetAuditLogsAsync<RestMemberBannedAuditLog>(5);
                    if (logs.OrderByDescending(x => x.Id).FirstOrDefault(x => x.TargetId == target.Id) is { } log)
                    {
                        moderator = await log.ResponsibleUser.DownloadAsync();
                        reason = log.Reason;
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

            if (await ctx.GetLoggingChannelAsync(guild.Id, LogType.Ban) is CachedTextChannel logChannel)
            {
                var message =
                    await SendLoggingEmbedAsync(ban, target, moderator, null, logChannel, guildConfig.Language);
                ban.SetLogMessage(message);
                ctx.Punishments.Update(ban);
                await ctx.SaveChangesAsync();
            }

            var user = await ctx.GetOrCreateGlobalUserAsync(target.Id);
            await SendTargetEmbedAsync(ban, target, null, user.Language);
        }

        public Task HandleAsync(MemberLeftEventArgs args)
            => LogKickAsync(args.User, args.Guild, null);

        public async Task LogKickAsync(IUser target, CachedGuild guild, Kick kick)
        {
            using var ctx = new AdminDatabaseContext(_provider);

            var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
            if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;

            var moderator = guild.CurrentMember as IUser;
            if (kick is null)
            {
                if (!guildConfig.Settings.HasFlag(GuildSettings.AutoPunishments) || KickedUserIds.Remove(target.Id))
                    return;

                string reason = null;
                // TODO: Adjust delay if necessary.
                await Task.Delay(TimeSpan.FromSeconds(1));

                var logs = await guild.GetAuditLogsAsync<RestMemberKickedAuditLog>(5);
                if (logs.OrderByDescending(x => x.Id).FirstOrDefault(x => x.TargetId == target.Id) is { } log)
                {
                    moderator = await log.ResponsibleUser.DownloadAsync();
                    reason = log.Reason;
                }

                kick = new Kick(guild.Id, target.Id, moderator.Id, reason);
                ctx.Punishments.Add(kick);
                await ctx.SaveChangesAsync();
            }
            else
            {
                moderator = await _client.GetOrDownloadUserAsync(kick.ModeratorId);
            }

            if (await ctx.GetLoggingChannelAsync(guild.Id, LogType.Kick) is CachedTextChannel logChannel)
            {
                var message =
                    await SendLoggingEmbedAsync(kick, target, moderator, null, logChannel, guildConfig.Language);
                kick.SetLogMessage(message);
                ctx.Punishments.Update(kick);
                await ctx.SaveChangesAsync();
            }

            var user = await ctx.GetOrCreateGlobalUserAsync(target.Id);
            await SendTargetEmbedAsync(kick, target, null, user.Language);
        }

        public async Task LogMuteAsync(IUser target, CachedGuild guild, IUser moderator, Mute mute)
        {
            using var ctx = new AdminDatabaseContext(_provider);

            var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
            if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;

            if (!(await ctx.GetLoggingChannelAsync(guild.Id, LogType.Mute) is CachedTextChannel logChannel))
                return;

            var message =
                await SendLoggingEmbedAsync(mute, target, moderator, null, logChannel, guildConfig.Language);
            mute.SetLogMessage(message);
            ctx.Punishments.Update(mute);
            await ctx.SaveChangesAsync();

            var user = await ctx.GetOrCreateGlobalUserAsync(target.Id);
            await SendTargetEmbedAsync(mute, target, null, user.Language);
        }

        public async Task LogWarningAsync(IUser target, CachedGuild guild, IUser moderator, Warning warning)
        {
            using var ctx = new AdminDatabaseContext(_provider);

            var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
            if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;

            if (await ctx.GetLoggingChannelAsync(guild.Id, LogType.Warn) is CachedTextChannel logChannel)
            {
                var message =
                    await SendLoggingEmbedAsync(warning, target, moderator, null, logChannel, guildConfig.Language);
                warning.SetLogMessage(message);
                ctx.Punishments.Update(warning);
                await ctx.SaveChangesAsync();
            }

            var user = await ctx.GetOrCreateGlobalUserAsync(target.Id);
            await SendTargetEmbedAsync(warning, target, null, user.Language);
        }

        public async Task LogAppealAsync(IUser target, CachedGuild guild, RevocablePunishment punishment)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            
            var guildConfig = await ctx.GetOrCreateGuildAsync(guild.Id);
            if (!guildConfig.Settings.HasFlag(GuildSettings.Punishments)) return;
            if (!(await ctx.GetLoggingChannelAsync(guild.Id, LogType.Appeal) is CachedTextChannel logChannel))
                return;

            await logChannel.SendMessageAsync(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(_localization.Localize(guildConfig.Language,
                               $"punishment_{punishment.GetType().Name.ToLower()}") +
                           $" - {_localization.Localize(guildConfig.Language, "punishment_case", punishment.Id)}")
                .WithDescription(_localization.Localize(guildConfig.Language, "punishment_appeal_description_guild",
                    target.Format()))
                .AddField("title_reason", punishment.AppealReason)
                .WithTimestamp(punishment.AppealedAt.Value).Build());
        }

        private async Task<IUserMessage> SendLoggingEmbedAsync(Punishment punishment, IUser target, IUser moderator, Punishment additionalPunishment, CachedTextChannel logChannel, LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new LocalEmbedBuilder()
                .WithErrorColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .AddField(_localization.Localize(language, "title_reason"),
                    punishment.Reason ?? _localization.Localize(language, "punishment_needsreason",
                        Markdown.Code(
                            $"{_config.DefaultPrefix}reason {punishment.Id} [{_localization.Localize(language, "title_reason").ToLower()}]")))
                .WithFooter(_localization.Localize(language, "punishment_moderator", moderator.Tag),
                    moderator.GetAvatarUrl())
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
                    Markdown.Bold(warningCount.ToOrdinalWords(language.Culture))));

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

            if (punishment.Format != ImageFormat.Default)
            {
                // TODO: Copying stream - is it necessary?
                var image = new MemoryStream(punishment.Image.ToArray());
                builder.WithImageUrl($"attachment://attachment.{punishment.Format.ToString().ToLower()}");
                return await logChannel.SendMessageAsync(new LocalAttachment(image,
                    $"attachment.{punishment.Format.ToString().ToLower()}"), embed: builder.Build());
            }

            return await logChannel.SendMessageAsync(embed: builder.Build());
        }

        public async Task SendLoggingRevocationEmbedAsync(RevocablePunishment punishment, IUser target, IUser moderator,
            CachedTextChannel logChannel, LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new LocalEmbedBuilder().WithWarnColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .WithDescription(_localization.Localize(language, $"punishment_{typeName}_revoke_description_guild",
                    $"**{target}** (`{target.Id}`)"))
                .AddField(_localization.Localize(language, "title_reason"),
                    punishment.RevocationReason ?? _localization.Localize(language, "punishment_noreason"))
                .WithFooter(_localization.Localize(language, "punishment_moderator", moderator.Tag),
                    moderator.GetAvatarUrl())
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
            var builder = new LocalEmbedBuilder().WithErrorColor()
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
                builder.WithDescription(_localization.Localize(language, "punishment_warning_description", 
                    Markdown.Bold(_client.GetGuild(punishment.GuildId).Name.Sanitize()),
                    Markdown.Bold(warningCount.ToOrdinalWords(language.Culture))));
            }
            else
            {
                builder.WithDescription(_localization.Localize(language, $"punishment_{typeName}_description",
                    _client.GetGuild(punishment.GuildId).Name));
            }

            if (punishment is RevocablePunishment revocable)
            {
                var field = new LocalEmbedFieldBuilder()
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
                        Markdown.Code(punishment.Id.ToString()),
                        Markdown.Code($"{_config.DefaultPrefix}appeal {punishment.Id} [{_localization.Localize(language, "title_reason").ToLower()}]"));
                }
            }

            if (punishment is Mute channelMute && channelMute.ChannelId.HasValue)
            {
                builder.AddField(_localization.Localize(language, "punishment_mute_channel"),
                    _client.GetGuild(punishment.GuildId).GetTextChannel(channelMute.ChannelId.Value).Mention);
            }

            if (punishment.Format != ImageFormat.Default)
            {
                builder.WithImageUrl($"attachment://attachment.{punishment.Format.ToString().ToLower()}");
                _ = target.SendMessageAsync(new LocalAttachment(punishment.Image,
                    $"attachment.{punishment.Format.ToString().ToLower()}"), embed: builder.Build());

                return;
            }

            _ = target.SendMessageAsync(embed: builder.Build());
        }

        public Task SendTargetRevocationEmbedAsync(RevocablePunishment punishment, IUser target,
            LocalizedLanguage language)
        {
            var typeName = punishment.GetType().Name.ToLower();
            var builder = new LocalEmbedBuilder().WithWarnColor()
                .WithTitle(_localization.Localize(language, $"punishment_{typeName}") +
                           $" - {_localization.Localize(language, "punishment_case", punishment.Id)}")
                .WithDescription(_localization.Localize(language, $"punishment_{typeName}_revoke_description", Markdown.Bold(_client.GetGuild(punishment.GuildId).Name.Sanitize())))
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

        public LocalEmbedFieldBuilder FormatAdditionalPunishment(Punishment punishment, LocalizedLanguage language)
        {
            var field = new LocalEmbedFieldBuilder()
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

        public async Task HandleAsync(MemberJoinedEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);

            var serverMuted = false;
            foreach (var mute in ctx.Punishments.OfType<Mute>().Where(x =>
                x.GuildId == args.Member.Guild.Id && x.TargetId == args.Member.Id && !x.IsExpired &&
                !x.RevokedAt.HasValue))
            {
                if (mute.ChannelId.HasValue)
                {
                    var channel = args.Member.Guild.GetTextChannel(mute.ChannelId.Value);
                    await channel.AddOrModifyOverwriteAsync(new LocalOverwrite(args.Member,
                        new OverwritePermissions().Deny(Permission.SendMessages).Deny(Permission.AddReactions)));
                    return;
                }

                if (!serverMuted && 
                    await ctx.GetSpecialRoleAsync(args.Member.Guild.Id, RoleType.Mute) is { } muteRole)
                {
                    await args.Member.GrantRoleAsync(muteRole.Id);
                    serverMuted = true;
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
                if (await ctx.GetLoggingChannelAsync(mute.GuildId, LogType.Unmute) is CachedTextChannel logChannel)
                {
                    await SendLoggingRevocationEmbedAsync(mute, target, _client.CurrentUser, logChannel,
                        guild.Language);
                }

                var user = await ctx.GetOrCreateGlobalUserAsync(mute.TargetId);
                _ = SendTargetRevocationEmbedAsync(mute, target, user.Language);

                if (mute.ChannelId.HasValue &&
                    _client.GetChannel(mute.ChannelId.Value) is CachedTextChannel muteChannel)
                {
                    _ = muteChannel.DeleteOverwriteAsync(target.Id);
                }
                else if (_client.GetGuild(mute.GuildId).GetMember(mute.TargetId) is CachedMember guildUser &&
                         await ctx.GetSpecialRoleAsync(mute.GuildId, RoleType.Mute) is CachedRole muteRole)
                {
                    _ = guildUser.RevokeRoleAsync(muteRole.Id);
                }
            }

            foreach (var ban in expiredBans)
            {
                var guild = await ctx.GetOrCreateGuildAsync(ban.GuildId);
                ban.Revoke(_client.CurrentUser.Id, _localization.Localize(guild.Language, "punishment_mute_expired"));
                ctx.Punishments.Update(ban);

                var target = await _client.GetOrDownloadUserAsync(ban.TargetId);
                if (await ctx.GetLoggingChannelAsync(ban.GuildId, LogType.Unban) is CachedTextChannel logChannel)
                {
                    await SendLoggingRevocationEmbedAsync(ban, target, _client.CurrentUser, logChannel,
                        guild.Language);
                }

                var user = await ctx.GetOrCreateGlobalUserAsync(ban.TargetId);
                _ = SendTargetRevocationEmbedAsync(ban, target, user.Language);

                _ = _client.GetGuild(ban.GuildId).UnbanMemberAsync(ban.TargetId);
            }

            await ctx.SaveChangesAsync();
        }

        Task IService.InitializeAsync()
            => _logging.LogInfoAsync("Initialized.", "Punishments");
    }
}