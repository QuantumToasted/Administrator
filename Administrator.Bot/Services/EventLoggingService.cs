using System.Collections.Concurrent;
using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Qommon;

namespace Administrator.Bot;

public sealed class EventLoggingService(IMemoryCache cache, AttachmentService attachmentService, InviteFilterService inviteFilter,
        AuditLogService auditLogs)
    : DiscordBotService
{
    private readonly ConcurrentDictionary<Snowflake, ConcurrentQueue<IMember>> _memberJoinQueues = new();
    private readonly ConcurrentDictionary<Snowflake, ConcurrentQueue<IUser>> _memberLeaveQueues = new();

    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.GuildId is not { } guildId)
            return;

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.LoggingChannels.FindAsync(guildId, LogEventType.MessageDelete) is null)
            return;

        if (e.Message is not IUserMessage {Attachments.Count: > 0} message)
            return;

        foreach (var attachment in message.Attachments.Take(5)) // arbitrarily stop at 5 attachments
        {
            try
            {
                if (!await attachmentService.CheckSizeAsync(attachment.Url, 8_000_000))
                    continue;

                var localAttachment = await attachmentService.GetAttachmentAsync(attachment.Url);
                cache.Set(attachment.Url, localAttachment, TimeSpan.FromMinutes(30));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to fetch attachment located at {Url}.", attachment.Url);
            }
        }
    }

    protected override async ValueTask OnMessageDeleted(MessageDeletedEventArgs e)
    {
        if (e.GuildId is not { } guildId)
            return;

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.LoggingChannels.FindAsync(guildId, LogEventType.MessageDelete) is not { } logChannel)
            return;

        var message = new LocalMessage()
            .AddComponent(LocalComponent.Row(LocalComponent.LinkButton(
                Discord.MessageJumpLink(guildId, e.ChannelId, e.MessageId), "Jump to message location")));

        var embed = new LocalEmbed()
            .WithCollectorsColor()
            .WithTitle("Message deleted")
            .AddField("Channel", $"{Mention.Channel(e.ChannelId)} ({Markdown.Bold(e.ChannelId)})")
            .AddField("Message ID", Markdown.Bold(e.MessageId))
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (e.Message is not null)
        {
            if (e.Message.Author.IsBot)
            {
                var guildConfig = await db.GetOrCreateGuildConfigAsync(guildId);
                if (guildConfig.HasSetting(GuildSettings.IgnoreBotMessages))
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
                    if (cache.TryGetValue<LocalAttachment>(attachment.Url, out var localAttachment))
                    {
                        message.AddAttachment(localAttachment!);
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

        if (auditLogs.GetAuditLog<IMessagesDeletedAuditLog>(guildId, 
                x => x.Id > e.MessageId && x.ChannelId == e.ChannelId) is {ActorId: { } actorId} log &&
            (log.Actor ?? Bot.GetUser(actorId)) is { } actor)
        {
            embed.AddField("Most likely responsible moderator", $"{actor.Tag} ({Markdown.Bold(actorId)})");
        }

        if (inviteFilter.DeletedMessageIds.Remove(e.MessageId))
        {
            embed.WithFooter("Automatically deleted by the invite filter.");
        }

        if (embed.Length >= Discord.Limits.Message.MaxEmbeddedContentLength)
        {
            embed.WithDescription(embed.Description.Value.Truncate(Discord.Limits.Message.MaxEmbeddedContentLength -
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
    }

    protected override async ValueTask OnMessageUpdated(MessageUpdatedEventArgs e)
    {
        if (e.GuildId is not { } guildId)
            return;

        var oldContent = e.OldMessage?.Content;
        var newContent = e.NewMessage?.Content ?? e.Model.Content.GetValueOrDefault();

        if (oldContent == newContent || (string.IsNullOrWhiteSpace(oldContent) && string.IsNullOrWhiteSpace(newContent)))
            return;

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.LoggingChannels.FindAsync(guildId, LogEventType.MessageUpdate) is not { } logChannel)
            return;

        var author = e.OldMessage?.Author ??
                     (e.Model.Author.HasValue ? new TransientUser(Bot, e.Model.Author.Value) : null) ??
                     e.NewMessage?.Author;

        if (author?.IsBot == true)
        {
            var guildConfig = await db.GetOrCreateGuildConfigAsync(guildId);
            if (guildConfig.HasSetting(GuildSettings.IgnoreBotMessages))
                return;
        }

        var message = new LocalMessage()
            .AddComponent(LocalComponent.Row(LocalComponent.LinkButton(
                Discord.MessageJumpLink(guildId, e.ChannelId, e.MessageId), "Jump to message location")));

        var embed = new LocalEmbed()
            .WithUniqueColor()
            .WithTitle("Message content updated")
            .AddField("Channel", $"{Mention.Channel(e.ChannelId)} ({Markdown.Code(e.ChannelId)})")
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
                ? oldContent.Truncate(Discord.Limits.Message.Embed.MaxDescriptionLength / 2)
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
                ? newContent.Truncate(Discord.Limits.Message.Embed.MaxDescriptionLength / 2)
                : Markdown.Italics("No content.")));
        }
        else if (e.Model.Content.HasValue)
        {
            embed.AddField(newContentField.WithValue(!string.IsNullOrWhiteSpace(e.Model.Content.Value)
                ? e.Model.Content.Value.Truncate(Discord.Limits.Message.Embed.MaxDescriptionLength / 2)
                : Markdown.Italics("No content.")));
        }
        else
        {
            embed.AddField(newContentField.WithValue(Markdown.Italics("Original message was not cached.")));
        }

        var i = 0;
        while (embed.Length >= Discord.Limits.Message.MaxEmbeddedContentLength && i++ <= 5)
        {
            if (newContentField.Value.Value.Length > 100)
                newContentField.WithValue(newContentField.Value.Value.Truncate(newContentField.Value.Value.Length / 2));

            if (oldContentField.Value.Value.Length > 100)
                newContentField.WithValue(oldContentField.Value.Value.Truncate(oldContentField.Value.Value.Length / 2));
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

    protected override ValueTask OnMemberJoined(MemberJoinedEventArgs e)
    {
        _memberJoinQueues.GetOrAdd(e.GuildId, _ => new ConcurrentQueue<IMember>()).Enqueue(e.Member);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnMemberLeft(MemberLeftEventArgs e)
    {
        _memberLeaveQueues.GetOrAdd(e.GuildId, _ => new ConcurrentQueue<IUser>()).Enqueue(e.User);
        return ValueTask.CompletedTask;
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
                        var attachment = await attachmentService.GetAttachmentAsync(e.OldMember.GetAvatarUrl(CdnAssetFormat.Automatic));
                        message.AddAttachment(new LocalAttachment(attachment.Stream, attachment.FileName));
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
                        var attachment = await attachmentService.GetAttachmentAsync(e.OldMember.GetGuildAvatarUrl(CdnAssetFormat.Automatic));
                        message.AddAttachment(new LocalAttachment(attachment.Stream, attachment.FileName));
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

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(LoopJoinQueuesAsync, stoppingToken);
        _ = Task.Run(LoopLeaveQueuesAsync, stoppingToken);
        return Task.CompletedTask;
    }

    private async Task LoopJoinQueuesAsync()
    {
        while (!Bot.StoppingToken.IsCancellationRequested)
        {
            //var queues = _memberJoinQueues.Values.ToList();
            foreach (var (guildId, queue) in _memberJoinQueues)
            {
                if (queue.IsEmpty)
                    continue;

                await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
                if (await db.LoggingChannels.FindAsync(guildId, LogEventType.Join) is not { } logChannel)
                {
                    queue.Clear();
                    continue;
                }

                var joinEmbeds = new List<LocalEmbed>();
                while (queue.TryDequeue(out var member))
                {
                    joinEmbeds.Add(FormatJoinEmbed(member));
                }

                foreach (var embedChunk in joinEmbeds.Chunk(5))
                {
                    try
                    {
                        await Bot.SendMessageAsync(logChannel.ChannelId, new LocalMessage().WithEmbeds(embedChunk));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to log join message with {EmbedCount} embed(s) to channel {ChannelId} in guild {GuildId}.",
                            embedChunk.Length, logChannel.ChannelId.RawValue, logChannel.GuildId.RawValue);
                        break; // don't try with the next chunk
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
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

    private async Task LoopLeaveQueuesAsync()
    {
        while (!Bot.StoppingToken.IsCancellationRequested)
        {
            foreach (var (guildId, queue) in _memberLeaveQueues)
            {
                if (queue.IsEmpty)
                    continue;

                await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
                if (await db.LoggingChannels.FindAsync(guildId, LogEventType.Leave) is not { } logChannel)
                {
                    queue.Clear();
                    continue;
                }

                var joinEmbeds = new List<LocalEmbed>();
                while (queue.TryDequeue(out var user))
                {
                    joinEmbeds.Add(FormatLeaveEmbed(user, Bot.GetGuild(guildId)));
                }

                foreach (var embedChunk in joinEmbeds.Chunk(5))
                {
                    try
                    {
                        await Bot.SendMessageAsync(logChannel.ChannelId, new LocalMessage().WithEmbeds(embedChunk));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to log leave message with {EmbedCount} embed(s) to channel {ChannelId} in guild {GuildId}.",
                            embedChunk.Length, logChannel.ChannelId.RawValue, logChannel.GuildId.RawValue);
                        break; // don't try with the next chunk
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
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
}