using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
    [Name("Moderation")]
    [RequireContext(ContextType.Guild)]
    public class ModerationCommands : AdminModuleBase
    {
        public PunishmentService Punishments { get; set; }

        public HttpClient Http { get; set; }

        [Command("ban")]
        [RequireBotPermissions(Permission.BanMembers)]
        [RequireUserPermissions(Permission.BanMembers)]
        public ValueTask<AdminCommandResult> BanUser([RequireHierarchy] CachedMember target,
            [Remainder] string reason = null)
            => BanUserAsync(target, reason);

        [Command("ban")]
        [RequireBotPermissions(Permission.BanMembers)]
        [RequireUserPermissions(Permission.BanMembers)]
        public async ValueTask<AdminCommandResult> BanUserAsync(ulong targetId, [Remainder] string reason = null)
        {
            if (Context.Guild.GetMember(targetId) is CachedMember target)
            {
                var result = await new RequireHierarchyAttribute().CheckAsync(target, Context);
                if (!result.IsSuccessful)
                {
                    return CommandError(result.Reason);
                }

                return await BanUser(target, reason);
            }
                
            if (!(await Context.Client.GetUserAsync(targetId) is RestUser restTarget))
                return CommandErrorLocalized("userparser_notfound");

            return await BanUserAsync(restTarget, reason);
        }

        private async ValueTask<AdminCommandResult> BanUserAsync(IUser target, string reason, Warning source = null)
        {
            if (!(await Context.Guild.GetBanAsync(target.Id) is null))
                return CommandErrorLocalized("moderation_alreadybanned", args: Markdown.Bold(target.Tag));

            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Ban ban = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                var image = new MemoryStream();
                var format = ImageFormat.Default;
                if (Context.Message.Attachments.FirstOrDefault() is { } attachment &&
                    attachment.FileName.HasImageExtension(out format))
                {
                    var stream = await Http.GetStreamAsync(attachment.Url);
                    await stream.CopyToAsync(image);
                    image.Seek(0, SeekOrigin.Begin);
                }

                ban = Context.Database.Punishments
                    .Add(new Ban(Context.Guild.Id, target.Id, Context.User.Id, reason, null, image, format)).Entity as Ban;
                await Context.Database.SaveChangesAsync();
                await Punishments.LogBanAsync(target, Context.Guild, ban);

                source?.SetSecondaryPunishment(ban);
            }

            await Context.Guild.BanMemberAsync(target.Id,
                FormatAuditLogReason(reason ?? Context.Localize("punishment_noreason"),
                    ban?.CreatedAt ?? DateTimeOffset.UtcNow, Context.User),
                7);

            Punishments.BannedUserIds.Add(target.Id);
            return CommandSuccessLocalized("moderation_ban",
                args: (ban is { } ? $"`[#{ban.Id}]` " : string.Empty) + target.Format());
        }

        [Command("tempban")]
        [RequireBotPermissions(Permission.BanMembers)]
        [RequireUserPermissions(Permission.BanMembers)]
        public ValueTask<AdminCommandResult> TempbanUser([RequireHierarchy] CachedMember target,
            TimeSpan duration, [Remainder] string reason = null)
            => TempbanUserAsync(target, duration, reason);

        [Command("tempban")]
        [RequireBotPermissions(Permission.BanMembers)]
        [RequireUserPermissions(Permission.BanMembers)]
        public async ValueTask<AdminCommandResult> TempbanUserAsync(ulong targetId, TimeSpan duration,
            [Remainder] string reason = null)
        {
            if (Context.Guild.GetMember(targetId) is CachedMember target)
            {
                var result = await new RequireHierarchyAttribute().CheckAsync(target, Context);
                if (!result.IsSuccessful)
                {
                    return CommandError(result.Reason);
                }

                return await TempbanUserAsync(target, duration, reason);
            }


            if (!(await Context.Client.GetUserAsync(targetId) is RestUser restTarget))
                return CommandErrorLocalized("userparser_notfound");

            return await TempbanUserAsync(restTarget, duration, reason);
        }

        private async ValueTask<AdminCommandResult> TempbanUserAsync(IUser target, TimeSpan duration, string reason, Warning source = null)
        {
            if (!(await Context.Guild.GetBanAsync(target.Id) is null))
                return CommandErrorLocalized("moderation_alreadybanned", args: Markdown.Bold(target.Tag));

            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Ban ban = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                var image = new MemoryStream();
                var format = ImageFormat.Default;
                if (Context.Message.Attachments.FirstOrDefault() is { } attachment &&
                    attachment.FileName.HasImageExtension(out format))
                {
                    var stream = await Http.GetStreamAsync(attachment.Url);
                    await stream.CopyToAsync(image);
                    image.Seek(0, SeekOrigin.Begin);
                }

                ban = Context.Database.Punishments
                    .Add(new Ban(Context.Guild.Id, target.Id, Context.User.Id, reason, duration, image, format)).Entity as Ban;
                await Context.Database.SaveChangesAsync();
                await Punishments.LogBanAsync(target, Context.Guild, ban);

                source?.SetSecondaryPunishment(ban);
            }

            await Context.Guild.BanMemberAsync(target.Id, 
                reason ?? FormatAuditLogReason(
                    ban?.Reason ?? Context.Localize("punishment_noreason"),
                    ban?.CreatedAt ?? DateTimeOffset.UtcNow, Context.User), 7);

            Punishments.BannedUserIds.Add(target.Id);

            return CommandSuccessLocalized("moderation_tempban",
                args: new object[]
                {
                    (ban is { } ? $"`[#{ban.Id}]` " : string.Empty) + target.Format(),
                    duration.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second)
                });
        }

        [Command("massban"), RunMode(RunMode.Parallel)]
        [RequireBotPermissions(Permission.BanMembers)]
        [RequireUserPermissions(Permission.BanMembers)]
        public async ValueTask<AdminCommandResult> MassBanAsync([Remainder] MassBan arguments = null)
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
                .AppendNewline()
                .AppendJoin('\n', targets.Select(x => x.Tag))
                .ToString();

            var password = Guid.NewGuid().ToString()[..4];
            if (targetString.Length > 1500)
            {
                var stream = new MemoryStream();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(targetString);
                await writer.FlushAsync();

                await Context.Channel.SendMessageAsync(new LocalAttachment(stream, "targets.txt"),
                    string.Join('\n', Localize("moderation_massban_target_count", targets.Count),
                        Markdown.CodeBlock(targetString),
                        Localize("moderation_masspunishment_confirmation",
                            Markdown.Code(password))));
            }
            else
            {
                await Context.Channel.SendMessageAsync(string.Join('\n', targets.Count > 1
                        ? Localize("moderation_massban_target_count", targets.Count)
                        : Localize("moderation_massban_target_single"), Markdown.CodeBlock(targetString),
                    Localize("moderation_masspunishment_confirmation",
                        Markdown.Code(password))));
            }

            var response = await GetNextMessageAsync();
            if (response?.Content.Equals(password, StringComparison.OrdinalIgnoreCase) != true)
            {
                return CommandErrorLocalized("info_timeout_password");
            }

            var counter = 0;
            var invoker = (CachedMember) Context.User;
            foreach (var target in targets)
            {
                if (await Context.Guild.GetBanAsync(target.Id) is { })
                {
                    if (arguments.IsVerbose)
                        await Context.Channel.SendMessageAsync(
                            Localize("moderation_massban_alreadybanned", target.Format()));
                    continue;
                }

                if (Context.Guild.GetMember(target.Id) is { } guildTarget)
                {
                    if (Context.Guild.CurrentMember.Hierarchy <= guildTarget.Hierarchy)
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
                    await Context.Guild.BanMemberAsync(target.Id, arguments.Reason, 7);
                    await Context.Channel.SendMessageAsync(duration.HasValue
                        ? Localize("moderation_tempban", target.Format(),
                            duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second))
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
            var invoker = (CachedMember)Context.User;
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
                else if (Context.Guild.GetMember(target.Id) is { } guildTarget)
                {
                    if (Context.Guild.CurrentMember.Hierarchy <= guildTarget.Hierarchy)
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
                        await Context.Guild.BanMemberAsync(target.Id, arguments.Reason, 7);
                        await Context.Channel.SendMessageAsync(duration.HasValue
                            ? Localize("moderation_tempban", target.Format(),
                                duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second))
                            : Localize("moderation_ban", target.Format()));
                    }
                }

                response = await GetNextMessageAsync(timeout: TimeSpan.FromSeconds(10));
            }

            return CommandSuccessLocalized("moderation_masspunishment_interactive_canceled");
        }

        [Command("kick")]
        [RequireBotPermissions(Permission.KickMembers)]
        [RequireUserPermissions(Permission.KickMembers)]
        public ValueTask<AdminCommandResult> KickUser([RequireHierarchy] CachedMember target,
            [Remainder] string reason = null)
            => KickUserAsync(target, reason);

        private async ValueTask<AdminCommandResult> KickUserAsync(CachedMember target, string reason,
            Warning source = null)
        {
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Kick kick = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                var image = new MemoryStream();
                var format = ImageFormat.Default;
                if (Context.Message.Attachments.FirstOrDefault() is { } attachment &&
                    attachment.FileName.HasImageExtension(out format))
                {
                    var stream = await Http.GetStreamAsync(attachment.Url);
                    await stream.CopyToAsync(image);
                    image.Seek(0, SeekOrigin.Begin);
                }

                kick = Context.Database.Punishments
                    .Add(new Kick(Context.Guild.Id, target.Id, Context.User.Id, reason, image, format)).Entity as Kick;
                await Context.Database.SaveChangesAsync();
                await Punishments.LogKickAsync(target, Context.Guild, kick);

                source?.SetSecondaryPunishment(kick);
            }

            await target.KickAsync(RestRequestOptions.FromReason(reason ?? Context.Localize("punishment_noreason")));

            Punishments.KickedUserIds.Add(target.Id);

            return CommandSuccessLocalized("moderation_kick",
                args: (kick is { } ? $"`[#{kick.Id}]` " : string.Empty) + target.Format());
        }

        [Command("mute")]
        [RequireBotPermissions(Permission.ManageRoles)]
        [RequireUserPermissions(Permission.ManageRoles, Group = "mute")]
        [RequireUserPermissions(Permission.MuteMembers, Group = "mute")]
        public ValueTask<AdminCommandResult> MuteUser([RequireHierarchy] CachedMember target, TimeSpan duration,
            [Remainder] string reason = null)
            => MuteUserAsync(target, duration, reason);

        [Command("mute")]
        [RequireBotPermissions(Permission.ManageRoles)]
        [RequireUserPermissions(Permission.ManageRoles, Group = "mute")]
        [RequireUserPermissions(Permission.MuteMembers, Group = "mute")]
        public ValueTask<AdminCommandResult> MuteUser([RequireHierarchy] CachedMember target,
            [Remainder] string reason = null)
            => MuteUserAsync(target, null, reason);

        private async ValueTask<AdminCommandResult> MuteUserAsync(CachedMember target, TimeSpan? duration,
            string reason, Warning source = null)
        {
            if (!(await Context.Database.GetSpecialRoleAsync(Context.Guild.Id, RoleType.Mute) is CachedRole muteRole))
                return CommandErrorLocalized("moderation_nomuterole");

            if (target.Roles.Keys.Any(x => x == muteRole.Id))
                return CommandErrorLocalized("moderation_alreadymuted");
            
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Mute mute = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                var image = new MemoryStream();
                var format = ImageFormat.Default;
                if (Context.Message.Attachments.FirstOrDefault() is { } attachment &&
                    attachment.FileName.HasImageExtension(out format))
                {
                    var stream = await Http.GetStreamAsync(attachment.Url);
                    await stream.CopyToAsync(image);
                    image.Seek(0, SeekOrigin.Begin);
                }

                mute = Context.Database.Punishments
                    .Add(new Mute(Context.Guild.Id, target.Id, Context.User.Id, reason, duration, null, image, format)).Entity as Mute;
                await Context.Database.SaveChangesAsync();
                await Punishments.LogMuteAsync(target, Context.Guild, Context.User, mute);

                source?.SetSecondaryPunishment(mute);
            }

            await target.GrantRoleAsync(muteRole.Id);

            Punishments.MutedUserIds.Add(target.Id);

            return duration.HasValue
                ? CommandSuccessLocalized("moderation_mute_temporary", args: new object[]
                {
                    (mute is { } ? $"`[#{mute.Id}]` " : string.Empty) + target.Format(),
                    duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second)
                })
                : CommandSuccessLocalized("moderation_mute",
                    args: (mute is { } ? $"`[#{mute.Id}]` " : string.Empty) + target.Format());
        }

        [Group("block", "channelmute")]
        [RequireBotPermissions(Permission.ManageRoles, Group = "bot")]
        [RequireBotPermissions(Permission.ManageRoles, false, Group = "bot")]
        [RequireUserPermissions(Permission.ManageRoles, false, Group = "user")]
        [RequireUserPermissions(Permission.ManageRoles, Group = "user")]
        [RequireUserPermissions(Permission.ManageChannels, false, Group = "user")]
        [RequireUserPermissions(Permission.ManageChannels, Group = "user")]
        [RequireUserPermissions(Permission.MuteMembers, false, Group = "user")]
        [RequireUserPermissions(Permission.MuteMembers, Group = "user")]
        public sealed class BlockCommands : ModerationCommands
        {
            [Command]
            public ValueTask<AdminCommandResult> BlockUser([RequireHierarchy] CachedMember target,
                [Remainder] string reason = null)
                => BlockUserAsync(target, Context.Channel as CachedTextChannel, null, reason);

            [Command]
            public ValueTask<AdminCommandResult> BlockUser([RequireHierarchy] CachedMember target,
                CachedTextChannel channel, [Remainder] string reason = null)
                => BlockUserAsync(target, channel, null, reason);

            [Command]
            public ValueTask<AdminCommandResult> BlockUser([RequireHierarchy] CachedMember target,
                TimeSpan duration, [Remainder] string reason = null)
                => BlockUserAsync(target, Context.Channel as CachedTextChannel, duration, reason);

            [Command]
            public ValueTask<AdminCommandResult> BlockUser([RequireHierarchy] CachedMember target,
                CachedTextChannel channel, TimeSpan duration, [Remainder] string reason = null)
                => BlockUserAsync(target, channel, duration, reason);

            private async ValueTask<AdminCommandResult> BlockUserAsync(CachedMember target, CachedTextChannel channel,
                TimeSpan? duration, string reason)
            {
                channel ??= (CachedTextChannel) Context.Channel;

                if (channel.Overwrites.Any(x => x.TargetId == target.Id))
                    return CommandErrorLocalized("moderation_alreadyblocked", args: Markdown.Bold(target.Tag));

                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                Mute mute = null;
                if (guild.Settings.HasFlag(GuildSettings.Punishments))
                {
                    var image = new MemoryStream();
                    var format = ImageFormat.Default;
                    if (Context.Message.Attachments.FirstOrDefault() is { } attachment &&
                        attachment.FileName.HasImageExtension(out format))
                    {
                        var stream = await Http.GetStreamAsync(attachment.Url);
                        await stream.CopyToAsync(image);
                        image.Seek(0, SeekOrigin.Begin);
                    }

                    mute = new Mute(Context.Guild.Id, target.Id, Context.User.Id, reason, duration, channel.Id, image, format);
                    if (channel.Overwrites.FirstOrDefault(x => x.TargetId == target.Id) is CachedOverwrite overwrite)
                    {
                        mute.StoreOverwrite(overwrite);
                        await channel.DeleteOverwriteAsync(target.Id);
                    }
                    mute = Context.Database.Punishments.Add(mute).Entity as Mute;
                    await Context.Database.SaveChangesAsync();
                    await Punishments.LogMuteAsync(target, Context.Guild, Context.User, mute);
                }

                await channel.AddOrModifyOverwriteAsync(new LocalOverwrite(target,
                    new OverwritePermissions().Deny(Permission.SendMessages).Deny(Permission.AddReactions)));

                return duration.HasValue
                    ? CommandSuccessLocalized("moderation_block_temporary", args: new object[]
                    {
                        (mute is { } ? $"`[#{mute.Id}]` " : string.Empty) + target.Format(),
                        channel.Mention,
                        duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second)
                    })
                    : CommandSuccessLocalized("moderation_block", args: new object[]
                    {
                        (mute is { } ? $"`[#{mute.Id}]` " : string.Empty) + target.Format(),
                        channel.Mention
                    });
            }
        }

        [Command("warn")]
        [RequireBotPermissions(Permission.KickMembers | Permission.BanMembers | Permission.ManageRoles)]
        [RequireUserPermissions(Permission.KickMembers, Group = "user")]
        [RequireUserPermissions(Permission.BanMembers, Group = "user")]
        [RequireUserPermissions(Permission.MuteMembers, Group = "user")]
        public async ValueTask<AdminCommandResult> WarnUserAsync([RequireHierarchy] CachedMember target,
            [Remainder] string reason = null)
        {
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            Warning warning = null;
            WarningPunishment extraPunishment = null;
            if (guild.Settings.HasFlag(GuildSettings.Punishments))
            {
                var image = new MemoryStream();
                var format = ImageFormat.Default;
                if (Context.Message.Attachments.FirstOrDefault() is { } attachment &&
                    attachment.FileName.HasImageExtension(out format))
                {
                    var stream = await Http.GetStreamAsync(attachment.Url);
                    await stream.CopyToAsync(image);
                    image.Seek(0, SeekOrigin.Begin);
                }

                warning = Context.Database.Punishments
                    .Add(new Warning(Context.Guild.Id, target.Id, Context.User.Id, reason, image, format))
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
                .Append($" | {Context.Localize("punishment_moderator", moderator.Tag)}")
                .Append($" | {timestamp.ToString("g", Context.Language.Culture)} UTC")
                .ToString();

        private async Task<List<IUser>> GetTargetsAsync(MassPunishment arguments)
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
                    targets = Context.Guild.Members.Values.Where(MatchesRegex)
                        .OrderByDescending(x => x.JoinedAt)
                        .Cast<IUser>()
                        .ToList();
                });

                using var _ = Context.Channel.Typing();
                var timeoutTask = await Task.WhenAny(delay, task);
                if (timeoutTask == delay)
                    throw new Exception(Localize("user_searchregex_timeout"));

                bool MatchesRegex(CachedMember target)
                {
                    if (string.IsNullOrWhiteSpace(target.Name)) return false;

                    if (string.IsNullOrWhiteSpace(target.Nick))
                        return regex.IsMatch(target.Name);

                    return regex.IsMatch(target.Name) || regex.IsMatch(target.Name);
                }
            }
            else if (arguments.Targets?.Count() > 0)
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