using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Commands
{
    [Name("Moderation")]
    [RequireContext(ContextType.Guild)]
    public class ModerationCommands : AdminModuleBase
    {
        public PunishmentService Punishments { get; set; }

        [Command("ban")]
        [RequireBotPermissions(GuildPermission.BanMembers)]
        [RequireUserPermissions(GuildPermission.BanMembers)]
        public ValueTask<AdminCommandResult> BanUser([RequireHierarchy] SocketGuildUser target,
            [Remainder] string reason = null)
            => BanUserAsync(target, reason);

        [Command("ban")]
        [RequireBotPermissions(GuildPermission.BanMembers)]
        [RequireUserPermissions(GuildPermission.BanMembers)]
        public async ValueTask<AdminCommandResult> BanUserAsync(ulong targetId, [Remainder] string reason = null)
        {
            if (Context.Guild.GetUser(targetId) is SocketGuildUser target)
            {
                var result = await new RequireHierarchyAttribute().CheckAsync(target, Context);
                if (!result.IsSuccessful)
                {
                    return CommandError(result.Reason);
                }

                return await BanUser(target, reason);
            }
                
            if (!(await Context.Client.Rest.GetUserAsync(targetId) is RestUser restTarget))
                return CommandErrorLocalized("userparser_notfound");

            return await BanUserAsync(restTarget, reason);
        }

        private async ValueTask<AdminCommandResult> BanUserAsync(IUser target, string reason, Warning source = null)
        {
            if (!(await Context.Guild.GetBanAsync(target) is null))
                return CommandErrorLocalized("moderation_alreadybanned", args: Format.Bold(target.ToString()));

            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Ban ban = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                ban = Context.Database.Punishments
                    .Add(new Ban(Context.Guild.Id, target.Id, Context.User.Id, reason, null)).Entity as Ban;
                await Context.Database.SaveChangesAsync();
                await Punishments.LogBanAsync(target, Context.Guild, ban);

                source?.SetSecondaryPunishment(ban);
            }

            await Context.Guild.AddBanAsync(target, 7, 
                FormatAuditLogReason(reason ?? Context.Localize("punishment_noreason"), ban?.CreatedAt ?? DateTimeOffset.UtcNow, Context.User));

            Punishments.BannedUserIds.Add(target.Id);
            return CommandSuccessLocalized("moderation_ban",
                args: (ban is { } ? $"`[#{ban.Id}]` " : string.Empty) + target.Format());
        }

        [Command("tempban")]
        [RequireBotPermissions(GuildPermission.BanMembers)]
        [RequireUserPermissions(GuildPermission.BanMembers)]
        public ValueTask<AdminCommandResult> TempbanUser([RequireHierarchy] SocketGuildUser target,
            TimeSpan duration, [Remainder] string reason = null)
            => TempbanUserAsync(target, duration, reason);

        [Command("tempban")]
        [RequireBotPermissions(GuildPermission.BanMembers)]
        [RequireUserPermissions(GuildPermission.BanMembers)]
        public async ValueTask<AdminCommandResult> TempbanUserAsync(ulong targetId, TimeSpan duration,
            [Remainder] string reason = null)
        {
            if (Context.Guild.GetUser(targetId) is SocketGuildUser target)
            {
                var result = await new RequireHierarchyAttribute().CheckAsync(target, Context);
                if (!result.IsSuccessful)
                {
                    return CommandError(result.Reason);
                }

                return await TempbanUserAsync(target, duration, reason);
            }


            if (!(await Context.Client.Rest.GetUserAsync(targetId) is RestUser restTarget))
                return CommandErrorLocalized("userparser_notfound");

            return await TempbanUserAsync(restTarget, duration, reason);
        }

        private async ValueTask<AdminCommandResult> TempbanUserAsync(IUser target, TimeSpan duration, string reason, Warning source = null)
        {
            if (!(await Context.Guild.GetBanAsync(target) is null))
                return CommandErrorLocalized("moderation_alreadybanned", args: Format.Bold(target.ToString()));

            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Ban ban = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                ban = Context.Database.Punishments
                    .Add(new Ban(Context.Guild.Id, target.Id, Context.User.Id, reason, duration)).Entity as Ban;
                await Context.Database.SaveChangesAsync();
                await Punishments.LogBanAsync(target, Context.Guild, ban);

                source?.SetSecondaryPunishment(ban);
            }

            await Context.Guild.AddBanAsync(target, 7,
                reason ?? FormatAuditLogReason(ban?.Reason ?? Context.Localize("punishment_noreason"), 
                    ban?.CreatedAt ?? DateTimeOffset.UtcNow, Context.User));

            Punishments.BannedUserIds.Add(target.Id);

            return CommandSuccessLocalized("moderation_tempban",
                args: new object[]
                {
                    (ban is { } ? $"`[#{ban.Id}]` " : string.Empty) + target.Format(),
                    duration.HumanizeFormatted(Context, TimeUnit.Second)
                });
        }

        [Command("massban"), RunMode(RunMode.Parallel)]
        [RequireBotPermissions(GuildPermission.BanMembers)]
        [RequireUserPermissions(GuildPermission.BanMembers)]
        public async ValueTask<AdminCommandResult> MassBanAsync(MassBan arguments = null)
        {
            arguments ??= new MassBan {IsInteractive = false};

            TimeSpan? duration = null;
            if (!string.IsNullOrWhiteSpace(arguments.DurationString))
            {
                duration = arguments.GetDuration(Context.Language.Culture);
                if (!duration.HasValue)
                    return CommandErrorLocalized("timespanparser_invalid");
            }

            if (arguments.IsInteractive)
                return await MassBanInteractiveAsync(arguments);

            List<IUser> targets;
            try
            {
                targets = await GetTargetsAsync(arguments);
            }
            catch (Exception ex)
            {
                return CommandError(ex.Message); // TODO: Shitty error handling
            }

            if (targets is null)
                return await MassBanInteractiveAsync(arguments);

            if (targets.Count == 0)
                return CommandErrorLocalized("moderation_massban_notargets");

            var targetString = new StringBuilder()
                .AppendLine()
                .AppendJoin('\n', targets.Select(x => x.ToString()))
                .ToString();

            var password = Guid.NewGuid().ToString()[..4];
            if (targetString.Length > 1500)
            {
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(targetString);
                await writer.FlushAsync();

                await Context.Channel.SendFileAsync(stream, "targets.txt",
                    string.Join('\n', Localize("moderation_massban_target_count", targets.Count),
                        Format.Code(targetString),
                        Localize("moderation_masspunishment_confirmation",
                            Format.Code(password))));
            }
            else
            {
                await Context.Channel.SendMessageAsync(string.Join('\n', targets.Count > 1
                        ? Localize("moderation_massban_target_count", targets.Count)
                        : Localize("moderation_massban_target_single"), Format.Code(targetString),
                    Localize("moderation_masspunishment_confirmation",
                        Format.Code(password))));
            }

            var response = await GetNextMessageAsync();
            if (response?.Content.Equals(password, StringComparison.OrdinalIgnoreCase) != true)
            {
                return CommandErrorLocalized("info_timeout_password");
            }

            var counter = 0;
            var invoker = (SocketGuildUser) Context.User;
            foreach (var target in targets)
            {
                if (await Context.Guild.GetBanAsync(target.Id) is { })
                {
                    if (arguments.IsVerbose)
                        await Context.Channel.SendMessageAsync(
                            Localize("moderation_massban_alreadybanned", target.Format()));
                    continue;
                }

                if (Context.Guild.GetUser(target.Id) is { } guildTarget)
                {
                    if (Context.Guild.CurrentUser.Hierarchy <= guildTarget.Hierarchy)
                    {
                        if (arguments.IsVerbose)
                            await Context.Channel.SendMessageAsync(Localize("moderation_massban_botpermissions", target.Format()));
                        continue;
                    }

                    if (invoker.Hierarchy <= guildTarget.Hierarchy)
                    {
                        if (arguments.IsVerbose)
                            await Context.Channel.SendMessageAsync(Localize("moderation_massban_userpermissions",
                                target.Format()));
                        continue;
                    }
                }

                if (arguments.CreateCases)
                {
                    var result = duration.HasValue
                        ? await TempbanUserAsync(target.Id, duration.Value, arguments.Reason)
                        : await BanUserAsync(target.Id, arguments.Reason);
                    await Context.Channel.SendMessageAsync(result.Text);
                }
                else
                {
                    await Context.Guild.AddBanAsync(target.Id, 7, arguments.Reason);
                    await Context.Channel.SendMessageAsync(duration.HasValue
                        ? Localize("moderation_tempban", target.Format(),
                            duration.Value.HumanizeFormatted(Context, TimeUnit.Second))
                        : Localize("moderation_ban", target.Format()));
                }

                counter++;
            }

            if (counter == 0)
                return CommandSuccessLocalized("moderation_massban_none");

            return counter > 1
                ? CommandSuccessLocalized("moderation_massban_multiple", args: counter)
                : CommandSuccessLocalized("moderation_massban");
        }

        private async ValueTask<AdminCommandResult> MassBanInteractiveAsync(MassBan arguments)
        {
            await Context.Channel.SendMessageAsync(Localize("moderation_masspunishment_interactive_prompt"));
            var invoker = (SocketGuildUser)Context.User;
            var duration = arguments.GetDuration(Context.Language.Culture);

            var response = await GetNextMessageAsync(timeout: TimeSpan.FromSeconds(10));

            while (!string.IsNullOrWhiteSpace(response?.Content))
            {
                if (!ulong.TryParse(response.Content, out var id))
                    break;

                var target = await Context.Client.GetOrDownloadUserAsync(id);

                var successful = true;
                if (await Context.Guild.GetBanAsync(target.Id) is { })
                {
                    await Context.Channel.SendMessageAsync(
                            Localize("moderation_massban_alreadybanned", target.Format()));
                    successful = false;
                }
                else if (Context.Guild.GetUser(target.Id) is { } guildTarget)
                {
                    if (Context.Guild.CurrentUser.Hierarchy <= guildTarget.Hierarchy)
                    {
                        await Context.Channel.SendMessageAsync(Localize("moderation_massban_botpermissions", target.Format()));
                        successful = false;
                    }
                    else if (invoker.Hierarchy <= guildTarget.Hierarchy)
                    {
                        await Context.Channel.SendMessageAsync(Localize("moderation_massban_userpermissions",
                                target.Format()));
                        successful = false;
                    }
                }
                
                if (successful)
                {
                    if (arguments.CreateCases)
                    {
                        var result = duration.HasValue
                            ? await TempbanUserAsync(target.Id, duration.Value, arguments.Reason)
                            : await BanUserAsync(target.Id, arguments.Reason);
                        await Context.Channel.SendMessageAsync(result.Text);
                    }
                    else
                    {
                        await Context.Guild.AddBanAsync(target.Id, 7, arguments.Reason);
                        await Context.Channel.SendMessageAsync(duration.HasValue
                            ? Localize("moderation_tempban", target.Format(),
                                duration.Value.HumanizeFormatted(Context, TimeUnit.Second))
                            : Localize("moderation_ban", target.Format()));
                    }
                }

                response = await GetNextMessageAsync(timeout: TimeSpan.FromSeconds(10));
            }

            return CommandSuccessLocalized("moderation_masspunishment_interactive_canceled");
        }

        [Command("kick")]
        [RequireBotPermissions(GuildPermission.KickMembers)]
        [RequireUserPermissions(GuildPermission.KickMembers)]
        public ValueTask<AdminCommandResult> KickUser([RequireHierarchy] SocketGuildUser target,
            [Remainder] string reason = null)
            => KickUserAsync(target, reason);

        private async ValueTask<AdminCommandResult> KickUserAsync(SocketGuildUser target, string reason,
            Warning source = null)
        {
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Kick kick = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                kick = Context.Database.Punishments
                    .Add(new Kick(Context.Guild.Id, target.Id, Context.User.Id, reason)).Entity as Kick;
                await Context.Database.SaveChangesAsync();
                await Punishments.LogKickAsync(target, Context.Guild, kick);

                source?.SetSecondaryPunishment(kick);
            }

            await target.KickAsync(reason ?? Context.Localize("punishment_noreason"));

            Punishments.KickedUserIds.Add(target.Id);

            return CommandSuccessLocalized("moderation_kick",
                args: (kick is { } ? $"`[#{kick.Id}]` " : string.Empty) + target.Format());
        }

        [Command("mute")]
        [RequireBotPermissions(GuildPermission.ManageRoles)]
        [RequireUserPermissions(GuildPermission.ManageRoles, Group = "mute")]
        [RequireUserPermissions(GuildPermission.MuteMembers, Group = "mute")]
        public ValueTask<AdminCommandResult> MuteUser([RequireHierarchy] SocketGuildUser target, TimeSpan duration,
            [Remainder] string reason = null)
            => MuteUserAsync(target, duration, reason);

        [Command("mute")]
        [RequireBotPermissions(GuildPermission.ManageRoles)]
        [RequireUserPermissions(GuildPermission.ManageRoles, Group = "mute")]
        [RequireUserPermissions(GuildPermission.MuteMembers, Group = "mute")]
        public ValueTask<AdminCommandResult> MuteUser([RequireHierarchy] SocketGuildUser target,
            [Remainder] string reason = null)
            => MuteUserAsync(target, null, reason);

        private async ValueTask<AdminCommandResult> MuteUserAsync(SocketGuildUser target, TimeSpan? duration,
            string reason, Warning source = null)
        {
            if (!(await Context.Database.GetSpecialRoleAsync(Context.Guild.Id, RoleType.Mute) is SocketRole muteRole))
                return CommandErrorLocalized("moderation_nomuterole");

            if (target.Roles.Any(x => x.Id == muteRole.Id))
                return CommandErrorLocalized("moderation_alreadymuted");
            
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Mute mute = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                mute = Context.Database.Punishments
                    .Add(new Mute(Context.Guild.Id, target.Id, Context.User.Id, reason, duration, null)).Entity as Mute;
                await Context.Database.SaveChangesAsync();
                await Punishments.LogMuteAsync(target, Context.Guild, Context.User, mute);

                source?.SetSecondaryPunishment(mute);
            }

            await target.AddRoleAsync(muteRole);

            Punishments.MutedUserIds.Add(target.Id);

            return duration.HasValue
                ? CommandSuccessLocalized("moderation_mute_temporary", args: new object[]
                {
                    (mute is { } ? $"`[#{mute.Id}]` " : string.Empty) + target.Format(),
                    duration.Value.HumanizeFormatted(Context, TimeUnit.Second)
                })
                : CommandSuccessLocalized("moderation_mute",
                    args: (mute is { } ? $"`[#{mute.Id}]` " : string.Empty) + target.Format());
        }

        [Group("block", "channelmute")]
        [RequireBotPermissions(GuildPermission.ManageRoles, Group = "bot")]
        [RequireBotPermissions(ChannelPermission.ManageRoles, Group = "bot")]
        [RequireUserPermissions(ChannelPermission.ManageRoles, Group = "user")]
        [RequireUserPermissions(GuildPermission.ManageRoles, Group = "user")]
        [RequireUserPermissions(ChannelPermission.ManageChannels, Group = "user")]
        [RequireUserPermissions(GuildPermission.ManageChannels, Group = "user")]
        [RequireUserPermissions(ChannelPermission.MuteMembers, Group = "user")]
        [RequireUserPermissions(GuildPermission.MuteMembers, Group = "user")]
        public sealed class BlockCommands : ModerationCommands
        {
            [Command]
            public ValueTask<AdminCommandResult> BlockUser([RequireHierarchy] SocketGuildUser target,
                [Remainder] string reason = null)
                => BlockUserAsync(target, Context.Channel as SocketTextChannel, null, reason);

            [Command]
            public ValueTask<AdminCommandResult> BlockUser([RequireHierarchy] SocketGuildUser target,
                SocketTextChannel channel, [Remainder] string reason = null)
                => BlockUserAsync(target, channel, null, reason);

            [Command]
            public ValueTask<AdminCommandResult> BlockUser([RequireHierarchy] SocketGuildUser target,
                TimeSpan duration, [Remainder] string reason = null)
                => BlockUserAsync(target, Context.Channel as SocketTextChannel, duration, reason);

            [Command]
            public ValueTask<AdminCommandResult> BlockUser([RequireHierarchy] SocketGuildUser target,
                SocketTextChannel channel, TimeSpan duration, [Remainder] string reason = null)
                => BlockUserAsync(target, channel, duration, reason);

            private async ValueTask<AdminCommandResult> BlockUserAsync(SocketGuildUser target, SocketTextChannel channel,
                TimeSpan? duration, string reason)
            {
                channel ??= (SocketTextChannel) Context.Channel;

                if (channel.PermissionOverwrites.Any(x => x.TargetId == target.Id))
                    return CommandErrorLocalized("moderation_alreadyblocked", args: Format.Bold(target.ToString()));

                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                Mute mute = null;
                if (guild.Settings.HasFlag(GuildSettings.Punishments))
                {
                    mute = new Mute(Context.Guild.Id, target.Id, Context.User.Id, reason, duration, channel.Id);
                    if (channel.PermissionOverwrites.FirstOrDefault(x => x.TargetId == target.Id) is Overwrite overwrite)
                    {
                        mute.StoreOverwrite(overwrite);
                        await channel.RemovePermissionOverwriteAsync(target);
                    }
                    mute = Context.Database.Punishments.Add(mute).Entity as Mute;
                    await Context.Database.SaveChangesAsync();
                    await Punishments.LogMuteAsync(target, Context.Guild, Context.User, mute);
                }

                await channel.AddPermissionOverwriteAsync(target,
                    new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny));

                return duration.HasValue
                    ? CommandSuccessLocalized("moderation_block_temporary", args: new object[]
                    {
                        (mute is { } ? $"`[#{mute.Id}]` " : string.Empty) + target.Format(),
                        channel.Mention,
                        duration.Value.HumanizeFormatted(Context, TimeUnit.Second)
                    })
                    : CommandSuccessLocalized("moderation_block", args: new object[]
                    {
                        (mute is { } ? $"`[#{mute.Id}]` " : string.Empty) + target.Format(),
                        channel.Mention
                    });
            }
        }

        [Command("warn")]
        [RequireBotPermissions(GuildPermission.KickMembers | GuildPermission.BanMembers | GuildPermission.ManageRoles)]
        [RequireUserPermissions(GuildPermission.KickMembers, Group = "user")]
        [RequireUserPermissions(GuildPermission.BanMembers, Group = "user")]
        [RequireUserPermissions(GuildPermission.MuteMembers, Group = "user")]
        public async ValueTask<AdminCommandResult> WarnUserAsync([RequireHierarchy] SocketGuildUser target,
            [Remainder] string reason = null)
        {
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Warning warning = null;
            WarningPunishment extraPunishment = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                warning = Context.Database.Punishments
                    .Add(new Warning(Context.Guild.Id, target.Id, Context.User.Id, reason))
                    .Entity as Warning;
                await Context.Database.SaveChangesAsync();
                var count = await Context.Database.Punishments.OfType<Warning>()
                    .CountAsync(x => x.GuildId == Context.Guild.Id && x.TargetId == target.Id && !x.RevokedAt.HasValue);

                extraPunishment = await Context.Database.WarningPunishments.FirstOrDefaultAsync(x =>
                    x.Count == count && x.GuildId == Context.Guild.Id);

                await Punishments.LogWarningAsync(target, Context.Guild, Context.User, warning);
            }

            await Context.Channel.SendMessageAsync(Context.Localize("moderation_warn",
                (warning is { } ? $"`[#{warning.Id}]` " : string.Empty) + target.Format()));

            if (!(extraPunishment is null) && !(warning is null))
            {
                reason = warning.LogMessageId == default
                    ? Context.Localize("moderation_additional_punishment", warning.Id)
                    : Context.Localize("moderation_additional_punishment_link", warning.Id,
                        $"https://discordapp.com/channels/{warning.GuildId}/{warning.LogMessageChannelId}/{warning.LogMessageId}");

                return extraPunishment.Type switch
                {
                    PunishmentType.Mute => await MuteUserAsync(target, extraPunishment.Duration, reason, warning),
                    PunishmentType.Kick => await KickUserAsync(target, reason, warning),
                    PunishmentType.Ban => extraPunishment.Duration.HasValue
                        ? await TempbanUserAsync(target, extraPunishment.Duration.Value, reason, warning)
                        : await BanUserAsync(target, reason, warning),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            return CommandSuccess();
        }

        private string FormatAuditLogReason(string reason, DateTimeOffset timestamp, IUser moderator)
            => new StringBuilder(reason ?? Context.Localize("punishment_noreason"))
                .Append($" | {Context.Localize("punishment_moderator", moderator.ToString())}")
                .Append($" | {timestamp.ToString("g", Context.Language.Culture)} UTC")
                .ToString();

        private async ValueTask<List<IUser>> GetTargetsAsync(MassPunishment arguments)
        {
            var targets = new List<IUser>();
            if (!string.IsNullOrWhiteSpace(arguments.RegexString))
            {
                Regex regex;
                try
                {
                    regex = new Regex(arguments.RegexString);
                }
                catch (FormatException)
                {
                    throw new Exception(Localize("regexparser_invalid", arguments.RegexString));
                }

                var delay = Task.Delay(TimeSpan.FromSeconds(5));
                var task = Task.Run(() =>
                {
                    targets = Context.Guild.Users.Where(MatchesRegex)
                        .OrderByDescending(x => x.JoinedAt ?? DateTimeOffset.UtcNow)
                        .Cast<IUser>()
                        .ToList();
                });

                using var _ = Context.Channel.EnterTypingState();
                var timeoutTask = await Task.WhenAny(delay, task);
                if (timeoutTask == delay)
                    throw new Exception(Localize("user_searchregex_timeout"));

                bool MatchesRegex(SocketGuildUser target)
                {
                    if (string.IsNullOrWhiteSpace(target.Username)) return false;

                    if (string.IsNullOrWhiteSpace(target.Nickname))
                        return regex.IsMatch(target.Username);

                    return regex.IsMatch(target.Nickname) || regex.IsMatch(target.Username);
                }
            }
            else if (arguments.Targets?.Length > 0)
            {
                foreach (var id in arguments.Targets)
                {
                    var target = await Context.Client.GetOrDownloadUserAsync(id);
                    if (target is { })
                        targets.Add(target);
                }
            }
            else
            {
                return null;
            }

            return targets;
        }
    }
}