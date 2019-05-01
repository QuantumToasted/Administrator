using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Services;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Commands.Modules.Moderation
{
    [Name("Moderation")]
    [RequireContext(ContextType.Guild)]
    public class ModerationCommands : AdminModuleBase
    {
        public PunishmentService Punishments { get; set; }

        [Command("ban", "b")]
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
                var result = await new RequireHierarchyAttribute().CheckAsync(target, Context, null);
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
            return CommandSuccessLocalized("moderation_ban", args: Format.Bold(target.ToString()));
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
                var result = await new RequireHierarchyAttribute().CheckAsync(target, Context, null);
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
                    Format.Bold(target.ToString()),
                    duration.Humanize(4, Context.Language.Culture, minUnit: TimeUnit.Minute)
                });
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
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                var kick = Context.Database.Punishments
                    .Add(new Kick(Context.Guild.Id, target.Id, Context.User.Id, reason)).Entity as Kick;
                await Context.Database.SaveChangesAsync();
                await Punishments.LogKickAsync(target, Context.Guild, kick);

                source?.SetSecondaryPunishment(kick);
            }

            await target.KickAsync(reason ?? Context.Localize("punishment_noreason"));

            Punishments.KickedUserIds.Add(target.Id);

            return CommandSuccessLocalized("moderation_kick",
                args: Format.Bold(target.ToString()));
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
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                var mute = new Mute(Context.Guild.Id, target.Id, Context.User.Id, reason, duration, null);
                Context.Database.Punishments.Add(mute);
                await Context.Database.SaveChangesAsync();
                await Punishments.LogMuteAsync(target, Context.Guild, Context.User, mute);

                source?.SetSecondaryPunishment(mute);
            }

            await target.AddRoleAsync(muteRole);

            Punishments.MutedUserIds.Add(target.Id);

            return duration.HasValue
                ? CommandSuccessLocalized("moderation_mute_temporary", args: new object[]
                {
                    Format.Bold(target.ToString()),
                    duration.Value.Humanize(4, Context.Language.Culture, minUnit: TimeUnit.Minute)
                })
                : CommandSuccessLocalized("moderation_mute", args: Format.Bold(target.ToString()));
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
                SocketTextChannel channel, TimeSpan duration, [Remainder] string reason = null)
                => BlockUserAsync(target, channel, duration, reason);

            private async ValueTask<AdminCommandResult> BlockUserAsync(SocketGuildUser target, SocketTextChannel channel,
                TimeSpan? duration, string reason)
            {
                channel ??= (SocketTextChannel) Context.Channel; 

                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                if (guild.Settings.HasFlag(GuildSettings.Punishments))
                {
                    var mute = new Mute(Context.Guild.Id, target.Id, Context.User.Id, reason, duration, channel.Id);
                    if (channel.PermissionOverwrites.FirstOrDefault(x => x.TargetId == target.Id) is Overwrite overwrite)
                    {
                        mute.StoreOverwrite(overwrite);
                        await channel.RemovePermissionOverwriteAsync(target);
                    }
                    Context.Database.Punishments.Add(mute);
                    await Context.Database.SaveChangesAsync();
                    await Punishments.LogMuteAsync(target, Context.Guild, Context.User, mute);
                }

                await channel.AddPermissionOverwriteAsync(target,
                    new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny));

                return duration.HasValue
                    ? CommandSuccessLocalized("moderation_block_temporary", args: new object[]
                    {
                        Format.Bold(target.ToString()),
                        channel.Mention,
                        duration.Value.Humanize(4, Context.Language.Culture, minUnit: TimeUnit.Minute)
                    })
                    : CommandSuccessLocalized("moderation_block", args: new object[]
                    {
                        Format.Bold(target.ToString()),
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

            await Context.Channel.SendMessageAsync(Context.Localize("moderation_warn", Format.Bold(target.ToString())));

            if (!(extraPunishment is null) && !(warning is null))
            {
                reason = Context.Localize("moderation_additional_punishment", warning.Id,
                    reason ?? Context.Localize("punishment_noreason"));
                switch (extraPunishment.Type)
                {
                    case PunishmentType.Mute:
                        await MuteUserAsync(target, extraPunishment.Duration, reason);
                        break;
                    case PunishmentType.Kick:
                        await KickUserAsync(target, reason);
                        break;
                    case PunishmentType.Ban:
                        if (extraPunishment.Duration.HasValue)
                        {
                            await TempbanUserAsync(target, extraPunishment.Duration.Value, reason);
                        }
                        else await BanUserAsync(target, reason);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return CommandSuccess();
        }

        private string FormatAuditLogReason(string reason, DateTimeOffset timestamp, IUser moderator)
            => new StringBuilder(reason ?? Context.Localize("punishment_noreason"))
                .Append($" | {Context.Localize("punishment_moderator", moderator.ToString())}")
                .Append($" | {timestamp.ToString("g", Context.Language.Culture)} UTC")
                .ToString();
    }
}