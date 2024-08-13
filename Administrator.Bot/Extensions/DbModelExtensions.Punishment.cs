using System.Diagnostics.CodeAnalysis;
using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qommon;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static bool CanBeAppealed(this RevocablePunishment punishment, [NotNullWhen(false)] out DateTimeOffset? appealAfter)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = (punishment as IExpiringDbEntity)?.ExpiresAt;
        
        if (!expires.HasValue)
        {
            if (now - punishment.CreatedAt < PunishmentService.MinimumAppealPermanentWaitDuration)
            {
                appealAfter = punishment.CreatedAt + PunishmentService.MinimumAppealPermanentWaitDuration;
                return false;
            }
        }
        else
        {
            var elapsed = now - punishment.CreatedAt;
            var total = expires.Value - punishment.CreatedAt;

            if (elapsed.TotalSeconds < PunishmentService.MINIMUM_APPEAL_WAIT_PERCENTAGE * total.TotalSeconds)
            {
                appealAfter = punishment.CreatedAt.AddSeconds(PunishmentService.MINIMUM_APPEAL_WAIT_PERCENTAGE * total.TotalSeconds);
                return false;
            }
        }

        appealAfter = null;
        return true;
    }
    
    public static LogEventType GetLogEventType(this Punishment punishment)
    {
        return punishment switch
        {
            Kick => LogEventType.Kick,
            Block => LogEventType.Block,
            Ban => LogEventType.Ban,
            TimedRole => LogEventType.TimedRole,
            Timeout => LogEventType.Timeout,
            Warning => LogEventType.Warning,
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        };
    }

    public static Task ApplyAsync(this Punishment punishment, DiscordBotBase bot, Member member)
    {
        var options = punishment.GetApplyRestRequestOptions();
        return punishment switch
        {
            Kick => bot.KickMemberAsync(punishment.GuildId, punishment.Target.Id, options),
            Block block => bot.SetOverwriteAsync(block.ChannelId, LocalOverwrite.Member(block.Target.Id,
                new OverwritePermissions().Deny(Permissions.SendMessages | Permissions.AddReactions))),
            Ban ban => bot.CreateBanAsync(ban.GuildId, ban.Target.Id, deleteMessageDays: ban.MessagePruneDays, options: options),
            TimedRole timedRole => timedRole.Mode is TimedRoleApplyMode.Grant
                ? bot.GrantRoleAsync(timedRole.GuildId, timedRole.Target.Id, timedRole.RoleId, options)
                : bot.RevokeRoleAsync(timedRole.GuildId, timedRole.Target.Id, timedRole.RoleId, options),
            Timeout timeout => bot.ModifyMemberAsync(punishment.GuildId, punishment.Target.Id, x => x.TimedOutUntil = timeout.ExpiresAt, options),
            Warning warning => ApplyWarningAsync(warning, bot, member),
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        };

        static Task ApplyWarningAsync(Warning warning, DiscordBotBase bot, Member member)
        {
            var decayService = bot.Services.GetRequiredService<DemeritPointDecayService>();
            member.DemeritPoints += warning.DemeritPoints;
            member.LastDemeritPointDecay = DateTimeOffset.UtcNow;
            decayService.CancelCts();
            return Task.CompletedTask;
        }
    }

    public static Task RevokeAsync(this RevocablePunishment punishment, DiscordBotBase bot)
    {
        var options = punishment.GetRevokeRestRequestOptions();
        return punishment switch
        {
            Ban => bot.DeleteBanAsync(punishment.GuildId, punishment.Target.Id, options),
            Block block => block.PreviousChannelAllowPermissions.HasValue
                ? bot.SetOverwriteAsync(block.ChannelId, LocalOverwrite.Member(block.Target.Id, new OverwritePermissions()
                    .Allow(block.PreviousChannelAllowPermissions.Value)
                    .Deny(block.PreviousChannelDenyPermissions!.Value)))
                : bot.DeleteOverwriteAsync(block.ChannelId, block.Target.Id),
            TimedRole timedRole => timedRole.Mode is TimedRoleApplyMode.Grant
                ? bot.RevokeRoleAsync(timedRole.GuildId, timedRole.Target.Id, timedRole.RoleId, options)
                : bot.GrantRoleAsync(timedRole.GuildId, timedRole.Target.Id, timedRole.RoleId, options),
            Timeout timeout => timeout.WasManuallyRevoked 
                ? bot.ModifyMemberAsync(timeout.GuildId, timeout.Target.Id, x => x.TimedOutUntil = null)
                : Task.CompletedTask,
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        };
    }
    
    public static async ValueTask<string> FormatCommandResponseStringAsync(this Punishment punishment, DiscordBotBase bot)
    {
        var builder = new StringBuilder($"{punishment} {Markdown.Bold(punishment.Target.Name)} ({Markdown.Code(punishment.Target.Id)}) has ");

        builder.Append(FormatPunishmentAction(punishment, bot))
            .AppendNewline(".");

        if (!string.IsNullOrWhiteSpace(punishment.Reason))
            builder.AppendNewline($"Reason: {punishment.Reason}");

        if (punishment is IExpiringDbEntity { ExpiresAt: { } expiresAt })
        {
            builder.AppendNewline($"Expires: {Markdown.Timestamp(expiresAt, Markdown.TimestampFormat.RelativeTime)}");
        }

        if (punishment is Warning { AdditionalPunishmentId: { } additionalPunishmentId } warning)
        {
            await using var scope = bot.Services.CreateAsyncScopeWithDatabase(out var db);
            var automaticPunishment = await db.AutomaticPunishments
                .Where(x => x.GuildId == punishment.GuildId && x.DemeritPoints >= warning.DemeritPointSnapshot)
                .OrderBy(x => x.DemeritPoints)
                .FirstAsync();

            builder.Append($"Automatic punishment {Markdown.Code($"[#{additionalPunishmentId}]")} (")
                .Append(FormatAutomaticPunishment(warning, automaticPunishment))
                .AppendNewline($") was also applied for reaching {Markdown.Bold("demerit point".ToQuantity(automaticPunishment.DemeritPoints))}.");

            static string FormatAutomaticPunishment(Punishment punishment, AutomaticPunishment automaticPunishment)
            {
                return automaticPunishment.PunishmentType switch
                {
                    PunishmentType.Ban when automaticPunishment.PunishmentDuration.HasValue =>
                        $"ban until {Markdown.Timestamp(punishment.CreatedAt + automaticPunishment.PunishmentDuration.Value, Markdown.TimestampFormat.LongDateTime)}",
                    PunishmentType.Ban =>
                        "permanent ban",
                    PunishmentType.Timeout =>
                        $"timeout until {Markdown.Timestamp(punishment.CreatedAt + automaticPunishment.PunishmentDuration!.Value, Markdown.TimestampFormat.LongDateTime)}",
                    PunishmentType.Kick =>
                        "kick",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        return builder.ToString();

        static string FormatPunishmentAction(Punishment punishment, DiscordBotBase bot)
        {
            return punishment switch
            {
                Kick => "left the server [kicked from server]",
                Block block => $"been blocked from {Mention.Channel(block.ChannelId)} ({Markdown.Code(block.ChannelId)})",
                Ban => "left the server [VAC banned from secure server]",
                TimedRole timedRole => FormatTimedRoleMessage(timedRole, bot),
                Timeout => "been gagged and muted [timed out]",
                Warning => "been given a warning",
                _ => throw new ArgumentOutOfRangeException(nameof(punishment), punishment, null)
            };
        }

        static string FormatTimedRoleMessage(TimedRole timedRole, DiscordBotBase bot)
        {
            var roleStr = bot.GetRole(timedRole.GuildId, timedRole.RoleId) is { } role
                ? $"{Markdown.Bold(role.Name)} ({role.Id})"
                : Mention.Role(timedRole.RoleId);

            return timedRole.Mode is TimedRoleApplyMode.Grant
                ? $"been given the temporary role {roleStr}"
                : $"had the role {roleStr} temporarily removed";
        }
    }
    
    public static async Task<TMessage> FormatLogMessageAsync<TMessage>(this Punishment punishment, DiscordBotBase bot)
        where TMessage : LocalMessageBase, new()
    {
        Guard.IsNotNull(punishment.Guild);
        
        var message = new TMessage();
        var embed = new LocalEmbed()
            .WithColor(punishment.GetInitialEmbedColor())
            .WithTitle(punishment.FormatEmbedTitle())
            .WithDescription(punishment.FormatLogEmbedDescription(bot))
            .AddField(punishment.FormatReasonField(bot, true))
            .WithTimestamp(punishment.CreatedAt);

        if (punishment is IExpiringDbEntity entity)
        {
            embed.AddField(entity.FormatExpiryField());
        }

        if (punishment.Guild.HasSetting(GuildSettings.LogModeratorsInPunishments))
        {
            var moderator = bot.GetMember(punishment.GuildId, punishment.Moderator.Id) ?? bot.GetUser(punishment.Moderator.Id);
            embed.WithFooter($"Moderator: {punishment.Moderator.Name}", moderator?.GetAvatarUrl());
        }

        if (punishment.Guild.HasSetting(GuildSettings.LogImagesInPunishments) && punishment.Attachment is { } attachment &&
            await attachment.DownloadAsync(bot) is { } localAttachment)
        {
            message.AddAttachment(localAttachment);
            embed.WithImageUrl($"attachment://{attachment.FileName}");
        }

        return message.AddEmbed(embed);
    }

    public static async Task<TMessage> FormatDmMessageAsync<TMessage>(this Punishment punishment, DiscordBotBase bot)
        where TMessage : LocalMessageBase, new()
    {
        Guard.IsNotNull(punishment.Guild);
        
        var message = new TMessage();
        var embed = new LocalEmbed()
            .WithColor(GetInitialEmbedColor(punishment))
            .WithTitle(punishment.FormatEmbedTitle())
            .WithDescription(punishment.FormatDmEmbedDescription(bot))
            .AddField(punishment.FormatReasonField(bot, false))
            .WithTimestamp(punishment.CreatedAt);

        if (punishment.Attachment is { } attachment && await attachment.DownloadAsync(bot) is { } localAttachment)
        {
            message.AddAttachment(localAttachment);
            embed.WithImageUrl($"attachment://{attachment.FileName}");
        }

        if (punishment is Warning { DemeritPoints: > 0 and var demeritPoints })
        {
            embed.AddField("Demerit points", demeritPoints);
        }
        
        if (punishment is IExpiringDbEntity entity)
        {
            embed.AddField(entity.FormatExpiryField());
        }

        if (!string.IsNullOrWhiteSpace(punishment.Guild.CustomPunishmentText))
        {
            embed.AddField("Message from moderators", punishment.Guild.CustomPunishmentText);
        }

        if (punishment is RevocablePunishment)
        {
            var mentions = bot.Services.GetRequiredService<SlashCommandMentionService>();
            var config = bot.Services.GetRequiredService<IOptions<AdministratorAppealConfiguration>>().Value;

            var appealWaitTextBuilder = new StringBuilder("You will be able to appeal this punishment ");
            if (punishment is IExpiringDbEntity { ExpiresAt: var expiresAt })
            {
                DateTimeOffset appealAfter;
                if (!expiresAt.HasValue)
                {
                    appealAfter = punishment.CreatedAt + PunishmentService.MinimumAppealPermanentWaitDuration;
                }
                else
                {
                    var total = expiresAt.Value - punishment.CreatedAt;
                    appealAfter = punishment.CreatedAt.AddSeconds(PunishmentService.MINIMUM_APPEAL_WAIT_PERCENTAGE * total.TotalSeconds);
                }

                appealWaitTextBuilder.AppendNewline(Markdown.Timestamp(appealAfter, Markdown.TimestampFormat.RelativeTime));
            }

            var appealGuild = bot.GetGuild(config.GuildId)!;
            embed.AddField("Appealing",
                    appealWaitTextBuilder +
                    $"To appeal this {punishment.GetType().Name.Humanize(LetterCasing.LowerCase)}, use the command {mentions.GetMention("appeal")} " +
                    $"with the ID {Markdown.Bold(punishment.Id)} or press the button below.")
                .AddField("Can't use the command?",
                    $"Join {Markdown.Link(appealGuild.Name, $"https://discord.gg/{config.GuildInviteCode}")} to share a server with the bot and be able to use the command.");
            
            message.AddComponent(new LocalRowComponent()
                .AddComponent(new LocalButtonComponent()
                    .WithCustomId($"Appeal:CreateModal:{punishment.Id}")
                    .WithLabel("Send Appeal")
                    .WithStyle(LocalButtonComponentStyle.Secondary)));
        }

        return message.AddEmbed(embed);
    }

    public static TMessage FormatAppealLogMessage<TMessage>(this RevocablePunishment punishment)
        where TMessage : LocalMessageBase, new()
    {
        var message = new TMessage();
        var embed = new LocalEmbed()
            .WithValveColor()
            .WithTitle(punishment.FormatEmbedTitle())
            .WithDescription(punishment.AppealStatus is AppealStatus.Sent or AppealStatus.NeedsInfo
                ? $"{Markdown.Bold(punishment.Target.Name)} ({Markdown.Code(punishment.Target.Id)}) has updated their {punishment.FormatPunishmentName(LetterCasing.LowerCase)} appeal."
                : $"{Markdown.Bold(punishment.Target.Name)} ({Markdown.Code(punishment.Target.Id)}) has appealed their {punishment.FormatPunishmentName(LetterCasing.LowerCase)}.")
            .AddField("Appeal", punishment.AppealText!)
            .WithTimestamp(punishment.AppealedAt ?? DateTimeOffset.UtcNow);

        message.AddEmbed(embed);
        
        message.AddComponent(LocalComponent.Row(
            LocalComponent.Button($"Appeal:Accept:{punishment.Id}", "Accept (Revoke)").WithStyle(LocalButtonComponentStyle.Success),
            LocalComponent.Button($"Appeal:NeedsInfo:{punishment.Id}", "Needs Info").WithStyle(LocalButtonComponentStyle.Primary),
            LocalComponent.Button($"Appeal:Reject:{punishment.Id}", "Reject").WithStyle(LocalButtonComponentStyle.Danger),
            LocalComponent.Button($"Appeal:Ignore:{punishment.Id}", "Ignore").WithStyle(LocalButtonComponentStyle.Secondary)));
        
        return message;
    }
    
    public static TMessage FormatAppealInfoNeededMessage<TMessage>(this RevocablePunishment punishment, DiscordBotBase bot)
        where TMessage : LocalMessageBase, new()
    {
        var mention = bot.Services.GetRequiredService<SlashCommandMentionService>();
        
        var message = new TMessage();
        var guild = bot.GetGuild(punishment.GuildId);
        var embed = new LocalEmbed()
            .WithValveColor()
            .WithTitle(punishment.FormatEmbedTitle())
            .WithDescription($"The moderators of {Markdown.Bold(guild!.Name)} have requested more information from your appeal.\n" + 
                             "This may be due to confusing or missing information in your original appeal.\n" +
                             $"You may appeal this punishment again using the {mention.GetMention("appeal")} command.")
            .AddField("Your original appeal", punishment.AppealText!)
            .WithTimestamp(punishment.AppealedAt ?? DateTimeOffset.UtcNow);
        
        message.AddEmbed(embed);
        return message;
    }

    public static TMessage FormatAppealRejectionMessage<TMessage>(this RevocablePunishment punishment, DiscordBotBase bot)
        where TMessage : LocalMessageBase, new()
    {
        var message = new TMessage();
        var guild = bot.GetGuild(punishment.GuildId);
        var embed = new LocalEmbed()
            .WithValveColor()
            .WithTitle(punishment.FormatEmbedTitle())
            .WithDescription($"The moderators of {Markdown.Bold(guild!.Name)} have rejected your appeal.\n" +
                             "As such, it can no longer be updated.\n" +
                             $"However, this does not mean this {punishment.FormatPunishmentName(LetterCasing.LowerCase)} will never be revoked.")
            .AddField("Your appeal", punishment.AppealText!)
            .WithTimestamp(punishment.AppealedAt ?? DateTimeOffset.UtcNow);

        message.AddEmbed(embed);
        return message;
    }
    
    public static TMessage FormatRevocationLogMessage<TMessage>(this RevocablePunishment punishment, DiscordBotBase bot)
        where TMessage : LocalMessageBase, new()
    {
        Guard.IsNotNull(punishment.Guild);
        
        var message = new TMessage();
        var embed = new LocalEmbed()
            .WithCommunityColor()
            .WithTitle(punishment.FormatEmbedTitle())
            .WithDescription(punishment.FormatRevocationLogEmbedDescription(bot))
            .AddField(punishment.FormatRevocationReasonField())
            .WithTimestamp(punishment.RevokedAt!.Value);
        
        if (punishment.Guild.HasSetting(GuildSettings.LogModeratorsInPunishments) && punishment.Revoker is { } revoker)
        {
            embed.WithFooter($"Revoker: {revoker.Name}", bot.GetMember(punishment.GuildId, revoker.Id)?.GetGuildAvatarUrl()
                                                        ?? bot.GetUser(revoker.Id)?.GetAvatarUrl());
        }
        
        message.AddEmbed(embed);
        return message;
    }
    
    public static TMessage FormatRevocationDmMessage<TMessage>(this RevocablePunishment punishment, DiscordBotBase bot)
        where TMessage : LocalMessageBase, new()
    {
        var message = new TMessage();
        var embed = new LocalEmbed()
            .WithCommunityColor()
            .WithTitle(punishment.FormatEmbedTitle())
            .WithDescription(punishment.FormatRevocationDmEmbedDescription(bot))
            .AddField(punishment.FormatRevocationReasonField())
            .WithTimestamp(punishment.RevokedAt!.Value);

        message.AddEmbed(embed);

        return message;
    }

    public static LocalEmbedField FormatPunishmentListEmbedField(this Punishment punishment, DiscordBotBase bot)
    {
        var field = new LocalEmbedField()
            .WithName(punishment.FormatEmbedTitle());

        var valueBuilder = new StringBuilder()
            .AppendNewline($"Target: {punishment.Target.Format()}")
            .AppendNewline($"Moderator: {punishment.Moderator.Format()}")
            .AppendNewline($"Reason: {punishment.Reason?.Truncate(100) ?? Markdown.Italics("No reason provided.")}")
            .AppendNewline($"Created: {Markdown.Timestamp(punishment.CreatedAt, Markdown.TimestampFormat.RelativeTime)}");

        if (punishment is Block { ChannelId: var channelId })
            valueBuilder.AppendNewline($"Channel: {Mention.Channel(channelId)}");
        
        if (punishment is TimedRole { RoleId: var roleId, Mode: var mode })
        {
            var verb = mode is TimedRoleApplyMode.Grant
                ? "granted"
                : "revoked";

            valueBuilder.AppendNewline($"Role {verb}: {Mention.Role(roleId)} ({Markdown.Code(roleId)})");
        }

        if (punishment is IExpiringDbEntity entity)
        {
            valueBuilder.Append(entity.ExpiresAt is { } expiresAt && expiresAt < DateTimeOffset.UtcNow
                    ? "Expired: "
                    : "Expires: ")
                .AppendNewline(entity.ExpiresAt.HasValue
                    ? Markdown.Timestamp(entity.ExpiresAt.Value, Markdown.TimestampFormat.RelativeTime)
                    : "never (permanent)");
        }

        if (punishment is RevocablePunishment revocablePunishment)
        {
            var emojis = bot.Services.GetRequiredService<EmojiService>();
            var yes = emojis.Names["white_check_mark"].ToString();
            var no = emojis.Names["x"].ToString();

            valueBuilder.Append("Appealed? ")
                .AppendNewline(revocablePunishment.AppealedAt.HasValue
                    ? $"{yes} ({revocablePunishment.AppealStatus?.Humanize(LetterCasing.Sentence) ?? "Unknown status"}): {revocablePunishment.AppealText}"
                        .Truncate(100)
                    : no);

            valueBuilder.Append("Revoked? ")
                .AppendNewline(revocablePunishment.RevokedAt.HasValue
                    ? $"{yes} {revocablePunishment.RevocationReason}"
                        .Truncate(100)
                    : no);
        }
        
        return field.WithValue(valueBuilder.ToString());
    }

    public static async Task<TMessage> FormatPunishmentCaseMessageAsync<TMessage>(this Punishment punishment, DiscordBotBase bot)
        where TMessage : LocalMessageBase, new()
    {
        var message = new TMessage();
        var embed = new LocalEmbed()
            .WithUnusualColor()
            .WithTitle(punishment.FormatEmbedTitle());

        if (punishment.Attachment is { } attachment && await attachment.DownloadAsync(bot) is { } localAttachment)
        {
            message.AddAttachment(localAttachment);
            
            if (attachment.FileName.Split('.')[^1].ToLower() is "png" or "jpeg" or "jpg" or "gif" or "webp")
            {
                embed.WithImageUrl($"attachment://{attachment.FileName}");
            }
        }

        var targetFieldValueBuilder = new StringBuilder();
        if (bot.GetUser(punishment.Target.Id) is { } currentTarget && currentTarget.Tag != punishment.Target.Tag)
            targetFieldValueBuilder.Append($"{Markdown.Bold(currentTarget.Tag)}, aka ");

        targetFieldValueBuilder.Append($"{Markdown.Bold(punishment.Target.Tag)} ({Markdown.Code(punishment.Target.Id)})");
        embed.AddField("Target", targetFieldValueBuilder.ToString());
        
        var moderatorFieldValueBuilder = new StringBuilder();
        if (bot.GetUser(punishment.Moderator.Id) is { } currentModerator && currentModerator.Tag != punishment.Moderator.Tag)
            moderatorFieldValueBuilder.Append($"{Markdown.Bold(currentModerator.Tag)}, aka ");

        moderatorFieldValueBuilder.Append($"{Markdown.Bold(punishment.Moderator.Tag)} ({Markdown.Code(punishment.Moderator.Id)})");
        embed.AddField("Responsible moderator", moderatorFieldValueBuilder.ToString());
        
        if (punishment is Block { ChannelId: var channelId })
            embed.AddField("Channel", $"{Mention.Channel(channelId)} ({Markdown.Code(channelId)})");

        if (punishment is TimedRole { RoleId: var roleId, Mode: var mode })
        {
            var verb = mode is TimedRoleApplyMode.Grant
                ? "granted"
                : "revoked";

            embed.AddField($"Role {verb}", $"{Mention.Role(roleId)} ({Markdown.Code(roleId)})");
        }

        if (punishment is IExpiringDbEntity entity)
            embed.AddField(entity.FormatExpiryField());

        if (punishment is RevocablePunishment revocablePunishment)
        {
            var emojis = bot.Services.GetRequiredService<EmojiService>();
            var yes = emojis.Names["white_check_mark"].ToString();
            var no = emojis.Names["x"].ToString();

            embed.AddField("Appealed?", revocablePunishment.AppealedAt.HasValue
                ? $"{yes}, {Markdown.Timestamp(revocablePunishment.AppealedAt.Value, Markdown.TimestampFormat.RelativeTime)}: {revocablePunishment.AppealText}"
                    .Truncate(Discord.Limits.Message.Embed.Field.MaxValueLength)
                : no);

            embed.AddField("Revoked?", revocablePunishment.RevokedAt.HasValue
                ? $"{yes}, {Markdown.Timestamp(revocablePunishment.RevokedAt.Value, Markdown.TimestampFormat.RelativeTime)} " +
                  $"(by {revocablePunishment.Revoker!.Format()}): {revocablePunishment.RevocationReason}"
                    .Truncate(Discord.Limits.Message.Embed.Field.MaxValueLength)
                : no);
        }
        
        embed.AddField("Created", Markdown.Timestamp(punishment.CreatedAt, Markdown.TimestampFormat.RelativeTime))
            .AddField(punishment.FormatReasonField(bot, false));

        return message.AddEmbed(embed);
    }
    
    public static string FormatPunishmentName(this Punishment punishment, LetterCasing casing = LetterCasing.Title)
        => punishment.GetType().Name.Humanize(casing);

    private static string FormatEmbedTitle(this Punishment punishment)
        => $"{punishment.FormatPunishmentName()} - Case #{punishment.Id}";

    private static LocalEmbedField FormatReasonField(this Punishment punishment, DiscordBotBase bot, bool isLogMessage)
    {
        var field = new LocalEmbedField().WithName("Reason");
        
        if (!string.IsNullOrWhiteSpace(punishment.Reason))
            return field.WithValue(punishment.Reason);

        var mentions = bot.Services.GetRequiredService<SlashCommandMentionService>();
        return field.WithValue(isLogMessage
            ? $"Moderator: use {mentions.GetMention("reason")} with case ID {Markdown.Bold(punishment.Id)}."
            : Markdown.Italics("No reason provided."));
    }
    
    private static LocalEmbedField FormatRevocationReasonField(this RevocablePunishment punishment)
    {
        return new LocalEmbedField().WithName("Reason")
            .WithValue(punishment.RevocationReason ?? Markdown.Italics("No reason provided."));
    }

    private static Color GetInitialEmbedColor(this Punishment punishment)
    {
        return punishment switch
        {
            Kick => Colors.Unique,
            Block => Colors.Vintage,
            Ban => Colors.Collectors,
            TimedRole => Colors.Haunted,
            Timeout => Colors.Strange,
            Warning => Colors.Normal,
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        };
    }

    private static string FormatLogEmbedDescription(this Punishment punishment, DiscordBotBase bot)
    {
        var builder = new StringBuilder($"{Markdown.Bold(punishment.Target.Name)} ({Markdown.Code(punishment.Target.Id)}) has ")
            .Append(punishment switch
            {
                Kick => "been kicked out",
                Block block => $"been blocked from {Mention.Channel(block.ChannelId)} ({Markdown.Code(block.ChannelId)})",
                Ban => "been banned",
                TimedRole timedRole => FormatTimedRoleAction(timedRole, bot),
                Timeout => "been given a timeout",
                Warning warning => FormatWarningAction(warning),
                _ => throw new ArgumentOutOfRangeException(nameof(punishment))
            })
            .Append('.');

        return builder.ToString();

        static string FormatWarningAction(Warning warning)
        {
            var builder = new StringBuilder("been given a warning");

            if (warning.DemeritPoints > 0)
                builder.Append($" worth {Markdown.Bold("demerit point".ToQuantity(warning.DemeritPoints))}");

            if (warning.DemeritPointSnapshot > 0)
                builder.AppendNewline(".").Append($"They are now at {Markdown.Bold("demerit point".ToQuantity(warning.DemeritPointSnapshot))}");

            return builder.ToString();
        }
        
        static string FormatTimedRoleAction(TimedRole timedRole, DiscordBotBase bot)
        {
            var roleStr = bot.GetRole(timedRole.GuildId, timedRole.RoleId) is { } role
                ? $"{Markdown.Bold(role.Name)} ({Markdown.Code(role.Id)})"
                : $"with ID {Markdown.Code(timedRole.RoleId)}";

            return timedRole.Mode is TimedRoleApplyMode.Grant
                ? $"been given the temporary role {roleStr}"
                : $"had the role {roleStr} temporarily removed";
        }
    }

    private static string FormatDmEmbedDescription(this Punishment punishment, DiscordBotBase bot)
    {
        var builder = new StringBuilder("You have ")
            .Append(punishment switch
            {
                Kick => "been kicked out of ",
                Block block => $"been blocked from {Markdown.Code(bot.GetChannel(block.GuildId, block.ChannelId)!.Name)} in ",
                Ban => "been banned from ",
                TimedRole timedRole => FormatTimedRoleAction(timedRole, bot),
                Timeout => "been given a timeout in ",
                Warning warning => FormatWarningAction(warning, bot),
                _ => throw new ArgumentOutOfRangeException(nameof(punishment))
            })
            .Append(punishment is not Warning ? Markdown.Bold(bot.GetGuild(punishment.GuildId)!.Name) : string.Empty)
            .Append('.');

        return builder.ToString();
        
        static string FormatWarningAction(Warning warning, DiscordBotBase bot)
        {
            //var builder = new StringBuilder($"been given a warning in {bot.GetGuild(warning.GuildId)!.Name}");
            // if (warning.DemeritPoints > 0)
            //    builder.Append($" worth {Markdown.Bold("demerit point".ToQuantity(warning.DemeritPoints))}");
            
            var builder = new StringBuilder("been given a warning ");
            if (warning.DemeritPoints > 0)
                builder.Append($" worth {Markdown.Bold("demerit point".ToQuantity(warning.DemeritPoints))} ");

            builder.Append($"in {bot.GetGuild(warning.GuildId)!.Name}");

            if (warning.DemeritPointSnapshot > 0)
                builder.AppendNewline(".").Append($"You are now at {Markdown.Bold("demerit point".ToQuantity(warning.DemeritPointSnapshot))}");

            return builder.ToString();
        }

        static string FormatTimedRoleAction(TimedRole timedRole, DiscordBotBase bot)
        {
            var roleStr = bot.GetRole(timedRole.GuildId, timedRole.RoleId) is { } role
                ? $"{Markdown.Bold(role.Name)} ({Markdown.Code(role.Id)})"
                : $"with ID {Markdown.Code(timedRole.RoleId)}";

            return timedRole.Mode is TimedRoleApplyMode.Grant
                ? $"been given the temporary role {roleStr} in "
                : $"had the role {roleStr} temporarily removed in ";
        }
    }

    private static string FormatRevocationLogEmbedDescription(this RevocablePunishment punishment, DiscordBotBase bot)
    {
        var builder = new StringBuilder($"{Markdown.Bold(punishment.Target.Name)} ({Markdown.Code(punishment.Target.Id)}) has ")
            .Append(punishment switch
            {
                Block block => $"been unblocked from {Mention.Channel(block.ChannelId)} ({Markdown.Code(block.ChannelId)})",
                Ban => "been unbanned",
                TimedRole timedRole => FormatTimedRoleAction(timedRole, bot),
                Timeout => "had a timeout removed",
                _ => throw new ArgumentOutOfRangeException(nameof(punishment))
            })
            .Append('.');

        return builder.ToString();

        static string FormatTimedRoleAction(TimedRole timedRole, DiscordBotBase bot)
        {
            var roleStr = bot.GetRole(timedRole.GuildId, timedRole.RoleId) is { } role
                ? $"{Markdown.Bold(role.Name)} ({Markdown.Code(role.Id)})"
                : $"with ID {Markdown.Code(timedRole.RoleId)}";

            return timedRole.Mode is TimedRoleApplyMode.Grant
                ? $"had the temporary role {roleStr} removed"
                : $"been given back the role {roleStr}";
        }
    }
    
    private static string FormatRevocationDmEmbedDescription(this RevocablePunishment punishment, DiscordBotBase bot)
    {
        var builder = new StringBuilder("You have ")
            .Append(punishment switch
            {
                Block block => $"been unblocked from {Markdown.Code(bot.GetChannel(block.GuildId, block.ChannelId)!.Name)} in ",
                Ban => "been unbanned from ",
                TimedRole timedRole => FormatTimedRoleAction(timedRole, bot),
                Timeout => "had a timeout removed in ",
                _ => throw new ArgumentOutOfRangeException(nameof(punishment))
            })
            .Append(Markdown.Bold(bot.GetGuild(punishment.GuildId)!.Name))
            .Append('.');

        return builder.ToString();

        static string FormatTimedRoleAction(TimedRole timedRole, DiscordBotBase bot)
        {
            var roleStr = bot.GetRole(timedRole.GuildId, timedRole.RoleId) is { } role
                ? $"{Markdown.Bold(role.Name)} ({role.Id})"
                : $"with ID {Markdown.Code(timedRole.RoleId)}";

            return timedRole.Mode is TimedRoleApplyMode.Grant
                ? $"had the temporary role {roleStr} removed in "
                : $"been given back the role {roleStr} in ";
        }
    }

    private static LocalEmbedField FormatExpiryField(this IExpiringDbEntity entity)
    {
        return new LocalEmbedField()
            .WithName(entity.ExpiresAt is { } expiresAt && expiresAt < DateTimeOffset.UtcNow
                ? "Expired"
                : "Expires")
            .WithValue(entity.ExpiresAt.HasValue
                ? Markdown.Timestamp(entity.ExpiresAt.Value, Markdown.TimestampFormat.RelativeTime)
                : "never (permanent)");
    }
    
    private static DefaultRestRequestOptions GetApplyRestRequestOptions(this Punishment punishment)
    {
        return new DefaultRestRequestOptions()
            .WithReason(($"{punishment.GetType().Name.Humanize(LetterCasing.Title)} #{punishment.Id}: " +
                         $"{punishment.Reason ?? "No reason provided."}").Truncate(Discord.Limits.Rest.MaxAuditLogReasonLength));
    }
    
    private static DefaultRestRequestOptions GetRevokeRestRequestOptions(this RevocablePunishment punishment)
    {
        return new DefaultRestRequestOptions()
            .WithReason(($"{punishment.GetType().Name.Humanize(LetterCasing.Title)} #{punishment.Id}: " +
                         $"{punishment.RevocationReason ?? "No reason provided."}").Truncate(Discord.Limits.Rest.MaxAuditLogReasonLength));
    }
}