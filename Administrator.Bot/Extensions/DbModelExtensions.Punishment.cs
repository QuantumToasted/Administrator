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
using Qommon;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static LogEventType GetLogEventType(this Punishment punishment)
    {
        return punishment.Type switch
        {
            PunishmentType.Ban => LogEventType.Ban,
            PunishmentType.Block => LogEventType.Block,
            PunishmentType.Kick => LogEventType.Kick,
            PunishmentType.TimedRole => LogEventType.TimedRole,
            PunishmentType.Timeout => LogEventType.Timeout,
            PunishmentType.Warning => LogEventType.Warning,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static Task ApplyAsync(this Punishment punishment, DiscordBotBase bot)
    {
        /* TODO: move this section to `/block` / PunishmentService
            var channel = await bot.FetchChannelAsync(.ChannelId) as ITextChannel;
            if (channel?.Overwrites.FirstOrDefault(x => x.TargetId == block.TargetId) is { } overwrite)
            {
                using var scope = bot.Services.CreateScope();
                await using var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

                PreviousChannelAllowPermissions = overwrite.Permissions.Allowed;
                PreviousChannelDenyPermissions = overwrite.Permissions.Denied;
                await db.SaveChangesAsync();
            }
        */
        
        var options = punishment.GetApplyRestRequestOptions();
        return punishment switch
        {
            Kick => bot.KickMemberAsync(punishment.GuildId, punishment.TargetId, options),
            Block block => bot.SetOverwriteAsync(block.ChannelId, LocalOverwrite.Member(block.TargetId,
                new OverwritePermissions().Deny(Permissions.SendMessages | Permissions.AddReactions))),
            Ban ban => bot.CreateBanAsync(ban.GuildId, ban.TargetId, deleteMessageDays: ban.MessagePruneDays, options: options),
            TimedRole timedRole => timedRole.Mode is TimedRoleApplyMode.Grant
                ? bot.GrantRoleAsync(timedRole.GuildId, timedRole.TargetId, timedRole.RoleId, options)
                : bot.RevokeRoleAsync(timedRole.GuildId, timedRole.TargetId, timedRole.RoleId, options),
            Timeout timeout => bot.ModifyMemberAsync(punishment.GuildId, punishment.TargetId, x => x.TimedOutUntil = timeout.ExpiresAt, options),
            Warning => Task.CompletedTask,
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        };

        

        /*
       static async Task ApplyWarningAsync(Warning warning, DiscordBotBase bot)
       {

           using var scope = bot.Services.CreateScope();
           await using var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
           var warningCount = await db.Punishments.OfType<Warning>()
               .CountAsync(x => x.GuildId == warning.GuildId && x.TargetId == warning.TargetId && !x.RevokedAt.HasValue);

           if (await db.WarningPunishments.FindAsync(warning.GuildId, warningCount) is not { } warningPunishment)
               return;

           //var punishmentService = bot.Services.GetRequiredService<PunishmentService>();
           var expiresAt = warning.CreatedAt + warningPunishment.PunishmentDuration;
           Punishment punishmentToApply = warningPunishment.PunishmentType switch
           {
               PunishmentType.Timeout => new Timeout(warning.GuildId, warning.TargetId, warning.TargetName, warning.ModeratorId,
                   warning.ModeratorName, $"Automatic timeout: See case {warning.FormatKey()}.", expiresAt!.Value),
               PunishmentType.Kick => new Kick(warning.GuildId, warning.TargetId, warning.TargetName, warning.ModeratorId,
                   warning.ModeratorName, $"Automatic timeout: See case {warning.FormatKey()}."),
               PunishmentType.Ban => new Ban(warning.GuildId, warning.TargetId, warning.TargetName, warning.ModeratorId,
                   warning.ModeratorName, $"Automatic timeout: See case {warning.FormatKey()}.", warning.Guild!.DefaultBanPruneDays, expiresAt),
               _ => throw new ArgumentOutOfRangeException()
           };

           await punishmentService.ProcessPunishmentAsync(punishmentToApply, null);
           AdditionalPunishmentId = punishmentToApply.Id;
           await db.SaveChangesAsync();
        }
        */
    }

    public static Task RevokeAsync(this RevocablePunishment punishment, DiscordBotBase bot)
    {
        var options = punishment.GetRevokeRestRequestOptions();
        return punishment switch
        {
            Ban => bot.DeleteBanAsync(punishment.GuildId, punishment.TargetId, options),
            Block block => block.PreviousChannelAllowPermissions.HasValue
                ? bot.SetOverwriteAsync(block.ChannelId, LocalOverwrite.Member(block.TargetId, new OverwritePermissions()
                    .Allow((Permissions) block.PreviousChannelAllowPermissions.Value)
                    .Deny((Permissions) block.PreviousChannelDenyPermissions!.Value)))
                : bot.DeleteOverwriteAsync(block.ChannelId, block.TargetId),
            TimedRole timedRole => timedRole.Mode is TimedRoleApplyMode.Grant
                ? bot.RevokeRoleAsync(timedRole.GuildId, timedRole.TargetId, timedRole.RoleId)
                : bot.GrantRoleAsync(timedRole.GuildId, timedRole.TargetId, timedRole.RoleId),
            Timeout timeout => timeout.WasManuallyRevoked 
                ? bot.ModifyMemberAsync(timeout.GuildId, timeout.TargetId, x => x.TimedOutUntil = null)
                : Task.CompletedTask,
            Warning => Task.CompletedTask,
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        };
    }
    
    public static async ValueTask<string> FormatCommandResponseStringAsync(this Punishment punishment, DiscordBotBase bot)
    {
        var builder = new StringBuilder($"{punishment.FormatKey()} {Markdown.Bold(punishment.TargetName)} ({Markdown.Code(punishment.TargetId)}) has");

        builder.Append(punishment switch
        {
            Kick => "left the server [kicked from server].",
            Block block => $"been blocked from {Mention.Channel(block.ChannelId)} ({Markdown.Code(block.ChannelId)}).",
            Ban => "left the server [VAC banned from secure server].",
            TimedRole timedRole => FormatTimedRoleMessage(timedRole, bot),
            Timeout => "been gagged and muted [timed out].",
            Warning warning => await FormatWarningMessageAsync(warning, bot),
            _ => ""
        });
        
        if (punishment is IExpiringDbEntity {ExpiresAt: { } expiresAt})
        {
            builder.AppendNewline().Append($"Expires: {Markdown.Timestamp(expiresAt, Markdown.TimestampFormat.RelativeTime)}");
        }

        return new(builder.ToString());

        static string FormatTimedRoleMessage(TimedRole timedRole, DiscordBotBase bot)
        {
            var roleStr = bot.GetRole(timedRole.GuildId, timedRole.RoleId) is { } role
                ? $"{Markdown.Bold(role.Name)} ({role.Id})"
                : Mention.Role(timedRole.RoleId);

            return timedRole.Mode is TimedRoleApplyMode.Grant
                ? $"been given the role {roleStr}."
                : $"had the role {roleStr} removed.";
        }

        static async Task<string> FormatWarningMessageAsync(Warning warning, DiscordBotBase bot)
        {
            if (warning.AdditionalPunishmentId.HasValue)
                Guard.IsNotNull(warning.AdditionalPunishment);

            await using var scope = bot.Services.CreateAsyncScopeWithDatabase(out var db);
            var warningCount = await db.Punishments.OfType<Warning>()
                .CountAsync(x => x.GuildId == warning.GuildId && x.TargetId == warning.TargetId && !x.RevokedAt.HasValue);

            var builder = new StringBuilder($"been given their {Markdown.Bold(warningCount.ToOrdinalWords())} warning.");
            if (warning.AdditionalPunishment is { } additionalPunishment)
            {
                var additionalPunishmentResponse = await additionalPunishment.FormatCommandResponseStringAsync(bot);
                builder.AppendNewline().Append(additionalPunishmentResponse);
            }

            return builder.ToString();
        }
    }
    
    public static TMessage FormatLogMessage<TMessage>(this Punishment punishment, DiscordBotBase bot)
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
            var moderator = bot.GetMember(punishment.GuildId, punishment.ModeratorId) ?? bot.GetUser(punishment.ModeratorId);
            embed.WithFooter($"Moderator: {punishment.ModeratorName}", moderator?.GetAvatarUrl());
        }

        if (punishment.Guild.HasSetting(GuildSettings.LogImagesInPunishments) && punishment.Attachment is { } attachment)
        {
            message.AddAttachment(attachment.ToLocalAttachment());
            embed.WithImageUrl($"attachment://{attachment.FileName}");
        }

        return message.AddEmbed(embed);
    }

    public static TMessage FormatDmMessage<TMessage>(this Punishment punishment, DiscordBotBase bot)
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

        if (punishment.Attachment is { } attachment)
        {
            message.AddAttachment(attachment.ToLocalAttachment());
            embed.WithImageUrl($"attachment://{attachment.FileName}");
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
            var config = bot.Services.GetRequiredService<IConfiguration>();
            
            var appealServer = new { Name = config["AppealGuild:Name"], InviteUrl = $"https://discord.gg/{config["AppealGuild:Code"]}" };
            embed.AddField("Appealing",
                    $"To appeal this {punishment.GetType().Name.Humanize(LetterCasing.LowerCase)}, use the command {mentions.GetMention("appeal")} " +
                    $"with the ID {Markdown.Bold(punishment.Id)}.")
                .AddField("Can't send the command?",
                    $"Join {Markdown.Link(appealServer.Name, appealServer.InviteUrl)} to share a server with the bot and be able to use the command.");
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
                ? $"{Markdown.Bold(punishment.TargetName)} ({punishment.TargetId}) has updated their {punishment.FormatPunishmentName(LetterCasing.LowerCase)} appeal."
                : $"{Markdown.Bold(punishment.TargetName)} ({punishment.TargetId}) has appealed their {punishment.FormatPunishmentName(LetterCasing.LowerCase)}.")
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
        
        if (punishment.Guild.HasSetting(GuildSettings.LogModeratorsInPunishments))
        {
            var revokerId = punishment.RevokerId.HasValue && punishment.RevokerId != bot.CurrentUser.Id
                ? punishment.RevokerId.Value
                : punishment.ModeratorId;
            
            var revokerName = punishment.RevokerId.HasValue && punishment.RevokerId != bot.CurrentUser.Id
                ? punishment.RevokerName!
                : punishment.ModeratorName;

            embed.WithFooter($"Revoker: {revokerName}", bot.GetMember(punishment.GuildId, revokerId)?.GetGuildAvatarUrl()
                                                        ?? bot.GetUser(revokerId)?.GetAvatarUrl());
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

    private static string FormatEmbedTitle(this Punishment punishment)
        => $"{punishment.FormatPunishmentName()} - Case #{punishment.Id}";

    private static string FormatPunishmentName(this Punishment punishment, LetterCasing casing = LetterCasing.Title)
        => punishment.GetType().Name.Humanize(casing);

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
        var builder = new StringBuilder($"{Markdown.Bold(punishment.TargetName)} ({Markdown.Code(punishment.TargetId)}) has ")
            .Append(punishment switch
            {
                Kick => "been kicked out",
                Block block => $"been blocked from {Mention.Channel(block.ChannelId)} ({Markdown.Code(block.ChannelId)})",
                Ban => "been banned",
                TimedRole timedRole => FormatTimedRoleAction(timedRole, bot),
                Timeout => "been given a timeout",
                Warning warning => FormatWarningAction(warning, bot),
                _ => throw new ArgumentOutOfRangeException(nameof(punishment))
            })
            .Append('.');

        return builder.ToString();

        static string FormatTimedRoleAction(TimedRole timedRole, DiscordBotBase bot)
        {
            var roleStr = bot.GetRole(timedRole.GuildId, timedRole.RoleId) is { } role
                ? $"{Markdown.Bold(role.Name)} ({role.Id})"
                : $"with ID {Markdown.Code(timedRole.RoleId)}";

            return timedRole.Mode is TimedRoleApplyMode.Grant
                ? $"been given the role {roleStr}"
                : $"had the role {roleStr} removed";
        }

        static string FormatWarningAction(Warning warning, DiscordBotBase bot)
        {
            using var scope = bot.Services.CreateAsyncScopeWithDatabase(out var db);
            
            var punishments = db.Punishments.AsNoTracking().Where(x => x.GuildId == warning.GuildId).ToList();
            var warningCount = punishments.OfType<Warning>().Count(x => x.TargetId == warning.TargetId && !x.RevokedAt.HasValue);

            return $" been given a warning.\nThis is their {Markdown.Bold(warningCount.ToOrdinalWords())} warning";
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
            .Append(Markdown.Bold(bot.GetGuild(punishment.GuildId)!.Name))
            .Append('.');

        return builder.ToString();

        static string FormatTimedRoleAction(TimedRole timedRole, DiscordBotBase bot)
        {
            var roleStr = bot.GetRole(timedRole.GuildId, timedRole.RoleId) is { } role
                ? $"{Markdown.Bold(role.Name)} ({role.Id})"
                : $"with ID {Markdown.Code(timedRole.RoleId)}";

            return timedRole.Mode is TimedRoleApplyMode.Grant
                ? $"been given the role {roleStr} in "
                : $"had the role {roleStr} removed in ";
        }

        static string FormatWarningAction(Warning warning, DiscordBotBase bot)
        {
            using var scope = bot.Services.CreateAsyncScopeWithDatabase(out var db);
            
            var punishments = db.Punishments.AsNoTracking().Where(x => x.GuildId == warning.GuildId).ToList();
            var warningCount = punishments.OfType<Warning>().Count(x => x.TargetId == warning.TargetId && !x.RevokedAt.HasValue);

            return $" been given a warning.\nThis is your {Markdown.Bold(warningCount.ToOrdinalWords())} warning in ";
        }
    }

    private static string FormatRevocationLogEmbedDescription(this RevocablePunishment punishment, DiscordBotBase bot)
    {
        var builder = new StringBuilder($"{Markdown.Bold(punishment.TargetName)} ({Markdown.Code(punishment.TargetId)}) has ")
            .Append(punishment switch
            {
                Block block => $"been unblocked from {Mention.Channel(block.ChannelId)} ({Markdown.Code(block.ChannelId)})",
                Ban => "been unbanned",
                TimedRole timedRole => FormatTimedRoleAction(timedRole, bot),
                Timeout => "been given a timeout",
                Warning => "had a warning revoked",
                _ => throw new ArgumentOutOfRangeException(nameof(punishment))
            })
            .Append('.');

        return builder.ToString();

        static string FormatTimedRoleAction(TimedRole timedRole, DiscordBotBase bot)
        {
            var roleStr = bot.GetRole(timedRole.GuildId, timedRole.RoleId) is { } role
                ? $"{Markdown.Bold(role.Name)} ({role.Id})"
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
                Warning warning => "had a warning revoked in ",
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

        static string FormatWarningAction(Warning warning, DiscordBotBase bot)
        {
            using var scope = bot.Services.CreateAsyncScopeWithDatabase(out var db);
            
            var punishments = db.Punishments.AsNoTracking().Where(x => x.GuildId == warning.GuildId).ToList();
            var warningCount = punishments.OfType<Warning>().Count(x => x.TargetId == warning.TargetId && !x.RevokedAt.HasValue);

            return $" been given a warning.\nThis is your {Markdown.Bold(warningCount.ToOrdinalWords())} warning in ";
        }
    }

    private static LocalEmbedField FormatExpiryField(this IExpiringDbEntity entity)
        => new LocalEmbedField().WithName("Expires").WithValue(entity.ExpiresAt.HasValue
            ? Markdown.Timestamp(entity.ExpiresAt.Value, Markdown.TimestampFormat.RelativeTime)
            : "never (permanent)");
    
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