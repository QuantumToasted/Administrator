using System.Collections.Concurrent;
using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Models;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qommon;
using StringExtensions = Administrator.Core.StringExtensions;

namespace Administrator.Bot;

public sealed class EventLoggingService : DiscordBotService
{
    private readonly BatchEventDispatcher<Snowflake, MemberJoinedEventArgs> _memberJoinDispatcher;
    private readonly BatchEventDispatcher<Snowflake, MemberLeftEventArgs> _memberLeaveDispatcher;
    private readonly InviteFilterService _inviteFilter;
    private readonly AuditLogService _auditLogs;
    private readonly MessageCacheService _messageCache;
    private readonly AttachmentServiceNew _attachments;

    public EventLoggingService(InviteFilterService inviteFilter, AuditLogService auditLogs, MessageCacheService messageCache,
        AttachmentServiceNew attachments)
    {
        _memberJoinDispatcher = new(HandleJoins);
        _memberLeaveDispatcher = new(HandleLeaves);

        _inviteFilter = inviteFilter;
        _auditLogs = auditLogs;
        _messageCache = messageCache;
        _attachments = attachments;
    }

    protected override async ValueTask OnMessageDeleted(MessageDeletedEventArgs e)
    {
        if (e.GuildId is not { } guildId)
            return;

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.LoggingChannels.FindAsync(guildId, LogEventType.MessageDelete) is not { } logChannel)
            return;

        _ = Task.Run(async () =>
        {
            var message = new LocalMessage()
                .AddComponent(LocalComponent.Row(LocalComponent.LinkButton(
                    Discord.MessageJumpLink(guildId, e.ChannelId, e.MessageId), "Jump to message location")));

            var embed = new LocalEmbed()
                .WithCollectorsColor()
                .WithTitle("Message deleted")
                .AddField("Channel", $"{Mention.Channel(e.ChannelId)}\n({Markdown.Code(e.ChannelId)})")
                .AddField("Message ID", Markdown.Bold(e.MessageId))
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (e.Message is not null)
            {
                if (e.Message.Author.IsBot)
                {
                    var settings = await db.Guilds.GetValueOrDefault(guildId, g => g.Settings);
                    //var guildConfig = await db.Guilds.GetOrCreateAsync(guildId);
                    if (settings.HasFlag(GuildSettings.IgnoreBotMessages))
                        return;
                }

                embed.WithAuthor($"{e.Message.Author} ({e.Message.Author.Id})",
                    (e.Message.Author as IMember)?.GetGuildAvatarUrl() ?? e.Message.Author.GetAvatarUrl());

                if (!string.IsNullOrWhiteSpace(e.Message.Content))
                    embed.WithDescription(e.Message.Content);

                if (e.Message.Attachments.Count > 0)
                {
                    embed.AddField("Attachments",
                        new StringBuilder().AppendJoinTruncated("\n", e.Message.Attachments.Select(x => x.Url),
                            Discord.Limits.Message.Embed.Field.MaxValueLength));

                    foreach (var attachment in e.Message.Attachments)
                    {
                        if (_attachments.GetFromCache(attachment.Url) is { } cachedAttachment)
                        {
                            message.AddAttachment(cachedAttachment.ToLocalAttachment());
                        }
                    }
                }

                if (e.Message.Stickers.Count > 0)
                {
                    embed.AddField("Stickers",
                        new StringBuilder().AppendJoinTruncated("\n",
                            e.Message.Stickers.Select(x => $"\"{x.Name}\" - {x.GetUrl()}"),
                            Discord.Limits.Message.Embed.Field.MaxValueLength));
                }
            }
            else
            {
                embed.WithFooter("This message was not cached, so no content can be displayed.");
            }

            /*
            var log = auditLogs.GetAuditLog<IMessagesDeletedAuditLog>(guildId,
                x => x.Id >= e.MessageId && x.ChannelId == e.ChannelId && x is { Count: 1, ActorId: not null });
            */

            var log = await _auditLogs.WaitForAuditLogAsync<IMessagesDeletedAuditLog>(guildId,
                x => x.Id >= e.MessageId && x.ChannelId == e.ChannelId && x is { Count: 1, ActorId: not null }, TimeSpan.FromSeconds(1));

            if (log is not null && (log.Actor ?? Bot.GetUser(log.ActorId!.Value)) is { } actor)
            {
                embed.AddField("Most likely responsible moderator", $"{actor.Tag} ({Markdown.Bold(actor.Id)})");
            }

            /*
            if (auditLogs.GetAuditLog<IMessagesDeletedAuditLog>(guildId,
                    x => x.Id >= e.MessageId && x.ChannelId == e.ChannelId && x.Count == 1) is { ActorId: { } actorId } log &&
                (log.Actor ?? Bot.GetUser(actorId)) is { } actor)
            {
                embed.AddField("Most likely responsible moderator", $"{actor.Tag} ({Markdown.Bold(actorId)})");
            }
            */

            if (_inviteFilter.DeletedMessageIds.Remove(e.MessageId))
            {
                embed.WithFooter("Automatically deleted by the invite filter.");
            }

            if (embed.Length >= Discord.Limits.Message.MaxEmbeddedContentLength)
            {
                embed.WithDescription(StringExtensions.Truncate(embed.Description.Value, Discord.Limits.Message.MaxEmbeddedContentLength -
                                                                                         (embed.Length - embed.Description.Value.Length)));
            }

            message.AddEmbed(embed);

            try
            {
                await Bot.SendMessageAsync(logChannel.ChannelId, message);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to log message {MessageId}'s deletion to channel {ChannelId} in guild {GuildId}.",
                    e.MessageId.RawValue, e.ChannelId.RawValue, guildId.RawValue);
            }
        });
    }

    protected override async ValueTask OnMessageUpdated(MessageUpdatedEventArgs e)
    {
        if (e.GuildId is not { } guildId)
            return;

        var oldContent = e.OldMessage?.Content;
        var newContent = e.NewMessage?.Content ?? e.Model.Content.GetValueOrDefault();

        var oldAttachments = e.OldMessage?.Attachments.Select(AttachmentReference.FromAttachment).ToList() ?? [];
        var newAttachments = e.NewMessage?.Attachments.Select(AttachmentReference.FromAttachment).ToList() 
                                 ?? e.Model.Attachments.GetValueOrDefault()?.Select(AttachmentReference.FromModel).ToList() ?? [];

        // content is unchanged
        // content is still null or whitespace
        // attachment count is same or larger (attachments not removed)
        if ((oldContent == newContent || (string.IsNullOrWhiteSpace(oldContent) && string.IsNullOrWhiteSpace(newContent))) 
            && oldAttachments.Count <= newAttachments.Count)
            return;

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.LoggingChannels.FindAsync(guildId, LogEventType.MessageUpdate) is not { } logChannel)
            return;

        var author = e.OldMessage?.Author ??
                     (e.Model.Author.HasValue ? new TransientUser(Bot, e.Model.Author.Value) : null) ??
                     e.NewMessage?.Author;

        if (author?.IsBot == true)
        {
            var settings = await db.Guilds.GetValueOrDefault(guildId, g => g.Settings);
            //var guildConfig = await db.Guilds.GetOrCreateAsync(guildId);
            if (settings.HasFlag(GuildSettings.IgnoreBotMessages))
                return;
        }

        var message = new LocalMessage()
            .AddComponent(LocalComponent.Row(LocalComponent.LinkButton(
                Discord.MessageJumpLink(guildId, e.ChannelId, e.MessageId), "Jump to message location")));

        var embed = new LocalEmbed()
            .WithUniqueColor()
            .WithTitle("Message content updated")
            .AddField("Channel", $"{Mention.Channel(e.ChannelId)}\n({Markdown.Code(e.ChannelId)})")
            .AddField("Message ID", Markdown.Code(e.MessageId))
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (author is not null)
        {
            embed.WithAuthor($"{author} ({author.Id})",
                (author as IMember)?.GetGuildAvatarUrl() ?? author.GetAvatarUrl());
        }

        var oldContentField = new LocalEmbedField().WithName("Old message content");
        if (e.OldMessage is not null)
        {
            embed.AddField(oldContentField.WithValue(!string.IsNullOrWhiteSpace(oldContent)
                ? StringExtensions.Truncate(oldContent, Discord.Limits.Message.Embed.Field.MaxValueLength)
                : Markdown.Italics("No content.")));
        }
        else
        {
            embed.AddField(oldContentField.WithValue(Markdown.Italics("Original message was not cached.")));
        }

        var newContentField = new LocalEmbedField().WithName("New message content");
        if (e.NewMessage is not null)
        {
            embed.AddField(newContentField.WithValue(!string.IsNullOrWhiteSpace(newContent)
                ? StringExtensions.Truncate(newContent, Discord.Limits.Message.Embed.Field.MaxValueLength)
                : Markdown.Italics("No content.")));
        }
        else if (e.Model.Content.HasValue)
        {
            embed.AddField(newContentField.WithValue(!string.IsNullOrWhiteSpace(e.Model.Content.Value)
                ? StringExtensions.Truncate(e.Model.Content.Value, Discord.Limits.Message.Embed.MaxDescriptionLength / 2)
                : Markdown.Italics("No content.")));
        }
        else
        {
            embed.AddField(newContentField.WithValue(Markdown.Italics("Original message was not cached.")));
        }

        if (oldAttachments.Count > newAttachments.Count)
        {
            var attachmentsRemovedBuilder = new StringBuilder();

            foreach (var removedAttachment in oldAttachments.Except(newAttachments))
            {
                if (_attachments.GetFromCache(removedAttachment.Url) is { } attachment)
                {
                    message.AddAttachment(attachment.ToLocalAttachment());
                    attachmentsRemovedBuilder.AppendNewline(Markdown.Code(attachment.FileName));
                }
                else
                {
                    attachmentsRemovedBuilder.AppendNewline(Markdown.Code(removedAttachment));
                }
            }

            embed.AddField("Attachments removed", attachmentsRemovedBuilder.ToString());
        }

        var i = 0;
        while (embed.Length >= Discord.Limits.Message.MaxEmbeddedContentLength && i++ <= 5)
        {
            if (newContentField.Value.Value.Length > 100)
                newContentField.WithValue(StringExtensions.Truncate(newContentField.Value.Value, newContentField.Value.Value.Length / 2));

            if (oldContentField.Value.Value.Length > 100)
                newContentField.WithValue(StringExtensions.Truncate(oldContentField.Value.Value, oldContentField.Value.Value.Length / 2));
        }

        message.AddEmbed(embed);

        try
        {
            await Bot.SendMessageAsync(logChannel.ChannelId, message);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to log message {MessageId}'s deletion to channel {ChannelId} in guild {GuildId}.",
                e.MessageId.RawValue, e.ChannelId.RawValue, guildId.RawValue);
        }
    }

    protected override async ValueTask OnMessagesDeleted(MessagesDeletedEventArgs e)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.LoggingChannels.FindAsync(e.GuildId, LogEventType.MessageDelete) is not { } logChannel)
            return;

        _ = Task.Run(async () =>
        {
            var contentBuilder = new StringBuilder();

            IAuditLog? log = null;
            var banLogSearchComplete = false;
            
            foreach (var messageId in e.MessageIds.Order())
            {
                var message = e.Messages.GetValueOrDefault(messageId);
                var cacheMessage = _messageCache.GetMessage(e.ChannelId, messageId);
                
                var author = message?.Author ?? cacheMessage?.Author;
                var content = message?.Content ?? cacheMessage?.Content ?? "NO DATA";
                
                if (string.IsNullOrWhiteSpace(content))
                    content = "NO CONTENT";
                
                contentBuilder.Append($"Message {messageId}: ");

                if (author is not null)
                {
                    contentBuilder.AppendNewline()
                        .Append($"{author.Tag} ({author.Id}): ");
                }

                contentBuilder.AppendNewline(content);
                
                foreach (var attachment in message?.Attachments ?? [])
                {
                    contentBuilder.AppendNewline(attachment.Url);
                }
                
                if (!banLogSearchComplete && author is not null)
                {
                    log = await _auditLogs.WaitForAuditLogAsync<IMemberBannedAuditLog>(e.GuildId, x => x.TargetId == author.Id,
                        TimeSpan.FromSeconds(2));
                    banLogSearchComplete = true;
                }

                contentBuilder.AppendNewline();
            }
            
            var localMessage = new LocalMessage()
                .AddAttachment(LocalAttachment.Bytes(Encoding.Default.GetBytes(contentBuilder.ToString()), $"BulkDelete_{Guid.NewGuid()}.txt"));

            var embed = new LocalEmbed()
                .WithCollectorsColor()
                .WithTitle("Messages bulk deleted")
                .AddField("Channel", $"{Mention.Channel(e.ChannelId)}\n({Markdown.Code(e.ChannelId)})")
                .AddField("Message count", e.MessageIds.Count)
                .WithTimestamp(DateTimeOffset.UtcNow);

            log ??= await _auditLogs.WaitForAuditLogAsync<IMessagesDeletedAuditLog>(e.GuildId,
                x => x.Id >= e.MessageIds[0] && x.ChannelId == e.ChannelId && x.Actor is not null && x.Count == e.MessageIds.Count,
                TimeSpan.FromSeconds(2));

            if (log is not null && (log.Actor ?? Bot.GetUser(log.ActorId!.Value)) is { } actor)
            {
                embed.AddField("Most likely responsible moderator", $"{actor.Tag} ({Markdown.Bold(actor.Id)})");
            }

            /*
            if (auditLogs.GetAuditLog<IMessagesDeletedAuditLog>(guildId,
                    x => x.Id >= e.MessageId && x.ChannelId == e.ChannelId && x.Count == 1) is { ActorId: { } actorId } log &&
                (log.Actor ?? Bot.GetUser(actorId)) is { } actor)
            {
                embed.AddField("Most likely responsible moderator", $"{actor.Tag} ({Markdown.Bold(actorId)})");
            }
            */

            localMessage.AddEmbed(embed);

            try
            {
                await Bot.SendMessageAsync(logChannel.ChannelId, localMessage);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to log a bulk message deletion to channel {ChannelId} in guild {GuildId}.",
                    e.ChannelId.RawValue, e.GuildId.RawValue);
            }
        });
    }

    protected override ValueTask OnMemberJoined(MemberJoinedEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            // TODO: due to how greeting messages are formatted, there is no way to bunch them up into single messages for large quantities of joins.
            // This may cause ratelimiting issues when raids occur in guilds....

            await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

            if (await db.Guilds.GetValueOrDefault(e.GuildId, g => g.GreetingMessage) is not { } greetingMessage)
                return;

            var channelId = await db.LoggingChannels.TryGetAsync(e.GuildId, LogEventType.Greeting);
            if (channelId is null)
            {
                var dm = await Bot.CreateDirectChannelAsync(e.MemberId);
                channelId = dm.Id;
            }

            var message = await greetingMessage.ToLocalMessageAsync<LocalMessage>(new DiscordPlaceholderFormatter(),
                new MockDiscordGuildCommandContext(Bot, e.GuildId, channelId.Value, e.Member));

            await Bot.TrySendMessageAsync(channelId.Value, message);
        });

        return _memberJoinDispatcher.WriteAsync(e.GuildId, e, Bot.StoppingToken);
    }

    protected override ValueTask OnMemberLeft(MemberLeftEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            // TODO: see OnMemberJoined w/r/t large amount of leave events

            await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
            if (await db.LoggingChannels.TryGetAsync(e.GuildId, LogEventType.Goodbye) is not { } channelId ||
                await db.Guilds.GetValueOrDefault(e.GuildId, g => g.GoodbyeMessage) is not { } goodbyeMessage)
                return;

            var message = await goodbyeMessage.ToLocalMessageAsync<LocalMessage>(new DiscordPlaceholderFormatter(),
                new MockDiscordGuildCommandContext(Bot, e.GuildId, channelId, e.User));

            await Bot.TrySendMessageAsync(channelId, message);
        });

        return _memberLeaveDispatcher.WriteAsync(e.GuildId, e, Bot.StoppingToken);
    }

    protected override async ValueTask OnMemberUpdated(MemberUpdatedEventArgs e)
    {
        if (e.OldMember is null)
            return;

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        if (await db.LoggingChannels.FindAsync(e.GuildId, LogEventType.AvatarUpdate) is { } avatarLogChannel)
        {
            var message = new LocalMessage();
            // user avatar update
            if (e.OldMember.AvatarHash != e.NewMember.AvatarHash)
            {
                var oldAvatarEmbed = new LocalEmbed()
                    .WithUnusualColor()
                    .WithTitle("User avatar updated")
                    .AddField("Old avatar", !string.IsNullOrWhiteSpace(e.OldMember.AvatarHash) ? e.OldMember.GetAvatarUrl() : Markdown.Italics("Default avatar."));

                if (e.OldMember.AvatarHash is not null)
                {
                    try
                    {
                        var attachment = await _attachments.GetAttachment(e.OldMember.GetAvatarUrl(CdnAssetFormat.Automatic));
                        message.AddAttachment(attachment.ToLocalAttachment());
                        oldAvatarEmbed.WithImageUrl($"attachment://{attachment.FileName}");
                    }
                    catch
                    {
                        oldAvatarEmbed.WithImageUrl(e.OldMember.GetAvatarUrl(CdnAssetFormat.Automatic))
                            .WithFooter("Original avatar was not cached and may not display properly.");
                    }
                }

                message.AddEmbed(oldAvatarEmbed);

                var newAvatarEmbed = new LocalEmbed()
                    .WithUnusualColor()
                    .AddField("New avatar", !string.IsNullOrWhiteSpace(e.NewMember.AvatarHash) 
                        ? e.NewMember.GetAvatarUrl() 
                        : Markdown.Italics("Removed (reset to default)."))
                    .WithImageUrl(e.NewMember.GetAvatarUrl(CdnAssetFormat.Automatic))
                    .WithTimestamp(DateTimeOffset.UtcNow);

                message.AddEmbed(newAvatarEmbed);
            }

            // user guild avatar update
            if (e.OldMember.GuildAvatarHash != e.NewMember.GuildAvatarHash)
            {
                var oldAvatarEmbed = new LocalEmbed()
                    .WithUnusualColor()
                    .WithTitle("User server avatar updated")
                    .AddField("Old server avatar", !string.IsNullOrWhiteSpace(e.OldMember.GuildAvatarHash) 
                        ? e.OldMember.GetGuildAvatarUrl() 
                        : Markdown.Italics("No server avatar."));

                if (e.OldMember.AvatarHash is not null)
                {
                    try
                    {
                        var attachment = await _attachments.GetAttachment(e.OldMember.GetGuildAvatarUrl(CdnAssetFormat.Automatic));
                        message.AddAttachment(attachment.ToLocalAttachment());
                        oldAvatarEmbed.WithImageUrl($"attachment://{attachment.FileName}");
                    }
                    catch
                    {
                        oldAvatarEmbed.WithImageUrl(e.OldMember.GetGuildAvatarUrl(CdnAssetFormat.Automatic))
                            .WithFooter("Original server avatar was not cached and may not display properly.");
                    }
                }

                message.AddEmbed(oldAvatarEmbed);

                var newAvatarEmbed = new LocalEmbed()
                    .WithUnusualColor()
                    .AddField("New server avatar", !string.IsNullOrWhiteSpace(e.NewMember.GuildAvatarHash) 
                        ? e.NewMember.GetGuildAvatarUrl() 
                        : Markdown.Italics("Server avatar removed."))
                    .WithImageUrl(e.NewMember.GetGuildAvatarUrl(CdnAssetFormat.Automatic))
                    .WithTimestamp(DateTimeOffset.UtcNow);

                message.AddEmbed(newAvatarEmbed);
            }

            if (message.Embeds.GetValueOrDefault()?.Count > 0)
            {
                try
                {
                    await Bot.SendMessageAsync(avatarLogChannel.ChannelId, message);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to send avatar update message for member {MemberId} to channel {ChannelId} in guild {GuildId}.",
                        e.MemberId.RawValue, avatarLogChannel.ChannelId.RawValue, e.GuildId.RawValue);
                }
            }
        }

        var usernameChanged = e.OldMember.Tag != e.NewMember.Tag;
        var nicknameChanged = e.OldMember.Nick != e.NewMember.Nick;
        if ((usernameChanged || nicknameChanged) &&
            await db.LoggingChannels.FindAsync(e.GuildId, LogEventType.NameUpdate) is { } nameLogChannel)
        {
            var message = new LocalMessage();

            if (usernameChanged)
            {
                message.AddEmbed(new LocalEmbed()
                    .WithUnusualColor()
                    .WithTitle("User name updated")
                    .WithAuthor($"{e.NewMember.Tag} ({e.MemberId})", e.NewMember.GetGuildAvatarUrl())
                    //.WithThumbnailUrl(e.NewMember.GetGuildAvatarUrl())
                    .AddField("Old username", e.OldMember.Tag)
                    .AddField("New username", e.NewMember.Tag)
                    .WithTimestamp(DateTimeOffset.UtcNow));
            }

            if (nicknameChanged)
            {
                message.AddEmbed(new LocalEmbed()
                    .WithUnusualColor()
                    .WithTitle("User nickname updated")
                    .WithAuthor($"{e.NewMember.Tag} ({e.MemberId})", e.NewMember.GetGuildAvatarUrl())
                    //.WithThumbnailUrl(e.NewMember.GetGuildAvatarUrl())
                    .AddField("Old nickname",
                        !string.IsNullOrWhiteSpace(e.OldMember.Nick)
                            ? Markdown.Escape(e.OldMember.Nick)
                            : Markdown.Italics("No nickname."))
                    .AddField("New nickname",
                        !string.IsNullOrWhiteSpace(e.NewMember.Nick)
                            ? Markdown.Escape(e.NewMember.Nick)
                            : Markdown.Italics("Nickname removed."))
                    .WithTimestamp(DateTimeOffset.UtcNow));
            }

            try
            {
                await Bot.SendMessageAsync(nameLogChannel.ChannelId, message);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to send name update message for member {MemberId} to channel {ChannelId} in guild {GuildId}.",
                    e.MemberId.RawValue, nameLogChannel.ChannelId.RawValue, e.GuildId.RawValue);
            }
        }

        // role update
        if (e.OldMember.RoleIds.SymmetricExceptWith(e.NewMember.RoleIds).Count > 0 &&
            await db.LoggingChannels.FindAsync(e.GuildId, LogEventType.UserRoleUpdate) is { } roleLogChannel)
        {
            var embed = new LocalEmbed()
                .WithUnusualColor()
                .WithTitle("User roles updated")
                .WithAuthor($"{e.NewMember.Tag} ({e.NewMember.Id})", e.NewMember.GetGuildAvatarUrl())
                .WithTimestamp(DateTimeOffset.UtcNow);

            var addedRoleIds = e.NewMember.RoleIds.Except(e.OldMember.RoleIds).ToList();
            if (addedRoleIds.Count > 0)
                embed.AddField("Roles added", new StringBuilder().AppendJoinTruncated(", ", addedRoleIds.Select(Mention.Role), Discord.Limits.Message.Embed.Field.MaxValueLength));

            var removedRoleIds = e.OldMember.RoleIds.Except(e.NewMember.RoleIds).ToList();
            if (removedRoleIds.Count > 0)
                embed.AddField("Roles removed", new StringBuilder().AppendJoinTruncated(", ", removedRoleIds.Select(Mention.Role), Discord.Limits.Message.Embed.Field.MaxValueLength));

            try
            {
                await Bot.SendMessageAsync(roleLogChannel.ChannelId, new LocalMessage().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to send role update message for member {MemberId} to channel {ChannelId} in guild {GuildId}.",
                    e.MemberId.RawValue, roleLogChannel.ChannelId.RawValue, e.GuildId.RawValue);
            }
        }
    }

    private async Task HandleJoins(ICollection<MemberJoinedEventArgs> batch, CancellationToken cancellationToken)
    {
        var guildId = batch.First().GuildId;
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.LoggingChannels.TryGetAsync(guildId, LogEventType.Join) is not { } channelId)
            return;
        
        var embeds = batch.Select(x => FormatJoinEmbed(x.Member)).ToList();
        
        try
        {
            await Bot.SendMessageAsync(channelId, new LocalMessage().WithEmbeds(embeds), cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to log join message with {EmbedCount} embed(s) to channel {ChannelId} in guild {GuildId}.",
                embeds.Count, channelId.RawValue, guildId.RawValue);
        }
        
        static LocalEmbed FormatJoinEmbed(IMember member)
        {
            var embed = new LocalEmbed()
                .WithUnusualColor()
                .WithTitle("Member joined")
                .WithDescription(member.Tag)
                .WithThumbnailUrl(member.GetGuildAvatarUrl())
                .AddField("Mention", member.Mention, true)
                .AddField("ID", member.Id, true)
                .AddField("Account created", Markdown.Timestamp(member.CreatedAt(), Markdown.TimestampFormat.RelativeTime), true)
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (member.GetGuild() is { } guild)
            {
                embed.WithFooter($"Member count: {guild.MemberCount}");
            }

            return embed;
        }
    }
    
    private async Task HandleLeaves(ICollection<MemberLeftEventArgs> batch, CancellationToken cancellationToken)
    {
        var guildId = batch.First().GuildId;
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.LoggingChannels.TryGetAsync(guildId, LogEventType.Leave) is not { } channelId)
            return;
        
        var embeds = batch.Select(x => FormatLeaveEmbed(x.User, x.Guild)).ToList();
        
        try
        {
            await Bot.SendMessageAsync(channelId, new LocalMessage().WithEmbeds(embeds), cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to log leave message with {EmbedCount} embed(s) to channel {ChannelId} in guild {GuildId}.",
                embeds.Count, channelId.RawValue, guildId.RawValue);
        }
        
        static LocalEmbed FormatLeaveEmbed(IUser user, CachedGuild? guild)
        {
            var embed = new LocalEmbed()
                .WithCollectorsColor()
                .WithTitle("Member left")
                .WithDescription(user.Tag)
                .WithThumbnailUrl(user.GetAvatarUrl())
                .AddField("Mention", user.Mention, true)
                .AddField("ID", user.Id, true)
                .AddField("Account created", Markdown.Timestamp(user.CreatedAt(), Markdown.TimestampFormat.RelativeTime), true)
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (guild is not null)
            {
                embed.WithFooter($"Member count: {guild.MemberCount}");
            }

            return embed;
        }
    }

    private record AttachmentReference(Snowflake Id, string Url)
    {
        public override int GetHashCode() => Id.GetHashCode();
        public virtual bool Equals(AttachmentReference? other) => other?.Id == Id;
        public override string ToString() => Url;
        public static AttachmentReference FromAttachment(IAttachment attachment) => new(attachment.Id, attachment.Url);
        public static AttachmentReference FromModel(AttachmentJsonModel attachment) => new(attachment.Id.Value, attachment.Url);
    }
}