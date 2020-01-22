using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.Events;
using Disqord.Rest.AuditLogs;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Services
{
    public sealed class EventLoggingService : Service,
        IHandler<MemberLeftEventArgs>,
        IHandler<MemberJoinedEventArgs>,
        IHandler<MessageDeletedEventArgs>,
        IHandler<MessageUpdatedEventArgs>,
        IHandler<MessageReceivedEventArgs>,
        IHandler<MemberUpdatedEventArgs>,
        IHandler<UserUpdatedEventArgs>,
        IHandler<ReactionRemovedEventArgs>
    {
        private readonly LoggingService _logging;
        private readonly HttpClient _http;
        private readonly LocalizationService _localization;
        private readonly Dictionary<Snowflake, LocalAttachment> _temporaryImages;

        public EventLoggingService(IServiceProvider provider)
            : base(provider)
        {
            _logging = _provider.GetRequiredService<LoggingService>();
            _http = _provider.GetRequiredService<HttpClient>();
            _localization = _provider.GetRequiredService<LocalizationService>();
            _temporaryImages = new Dictionary<Snowflake, LocalAttachment>();
        }

        public async Task HandleAsync(MessageUpdatedEventArgs args)
        {
            if (!(args.Channel is CachedTextChannel channel) ||
                !args.OldMessage.HasValue || // TODO: What to do when message doesn't have value, other than ignore
                args.OldMessage.Value.Content?.Equals(args.NewMessage.Content ?? string.Empty) == true) // message content is identical
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            var guild = await ctx.GetOrCreateGuildAsync(channel.Guild.Id);
            if (!(await ctx.GetLoggingChannelAsync(channel.Guild.Id, LogType.MessageUpdate) is { } logChannel))
                return;

            var builder = new LocalEmbedBuilder()
                .WithErrorColor()
                .WithTitle(_localization.Localize(guild.Language, "logging_message_update", channel.Tag))
                .WithDescription(args.NewMessage.Author.Format(false))
                .AddField(_localization.Localize(guild.Language, "info_id"), args.NewMessage.Id)
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (!string.IsNullOrWhiteSpace(args.OldMessage.Value.Content))
            {
                builder.AddField(_localization.Localize(guild.Language, "logging_message_update_oldcontent"),
                    args.OldMessage.Value.Content.TrimTo(1024, true));
            }

            if (!string.IsNullOrWhiteSpace(args.NewMessage.Content))
            {
                builder.AddField(_localization.Localize(guild.Language, "logging_message_update_newcontent"),
                    args.NewMessage.Content.TrimTo(1024, true));
            }

            await logChannel.SendMessageAsync(embed: builder.Build());
        }

        public async Task HandleAsync(MemberLeftEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            var guild = await ctx.GetOrCreateGuildAsync(args.Guild.Id);
            if (!(await ctx.GetLoggingChannelAsync(args.Guild.Id, LogType.Leave) is { } logChannel))
                return;

            await logChannel.SendMessageAsync(embed: new LocalEmbedBuilder()
                .WithErrorColor()
                .WithThumbnailUrl(args.User.GetAvatarUrl())
                .WithTitle(_localization.Localize(guild.Language, "logging_member_left"))
                .WithDescription(args.User.Tag.Sanitize())
                .AddField(_localization.Localize(guild.Language, "info_mention"), args.User.Mention)
                .AddField(_localization.Localize(guild.Language, "info_id"), args.User.Id)
                .AddField(_localization.Localize(guild.Language, "info_created"), string.Join('\n',
                    args.User.Id.CreatedAt.ToString("g", guild.Language.Culture),
                    (DateTimeOffset.UtcNow - args.User.Id.CreatedAt).HumanizeFormatted(_localization, guild.Language,
                        TimeUnit.Second, true)))
                .WithFooter(_localization.Localize(guild.Language, "logging_membercount", args.Guild.MemberCount))
                .WithTimestamp(DateTimeOffset.UtcNow).Build());
        }

        public async Task HandleAsync(MemberJoinedEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            var guild = await ctx.GetOrCreateGuildAsync(args.Member.Guild.Id);
            if (!(await ctx.GetLoggingChannelAsync(args.Member.Guild.Id, LogType.Join) is { } logChannel))
                return;

            await logChannel.SendMessageAsync(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithThumbnailUrl(args.Member.GetAvatarUrl())
                .WithTitle(_localization.Localize(guild.Language, "logging_member_join"))
                .WithDescription(args.Member.Tag.Sanitize())
                .AddField(_localization.Localize(guild.Language, "info_mention"), args.Member.Mention)
                .AddField(_localization.Localize(guild.Language, "info_id"), args.Member.Id)
                .AddField(_localization.Localize(guild.Language, "info_created"), string.Join('\n',
                    args.Member.Id.CreatedAt.ToString("g", guild.Language.Culture),
                    (DateTimeOffset.UtcNow - args.Member.Id.CreatedAt).HumanizeFormatted(_localization, guild.Language,
                        TimeUnit.Second, true)))
                .WithFooter(_localization.Localize(guild.Language, "logging_membercount", args.Member.Guild.MemberCount))
                .WithTimestamp(args.Member.JoinedAt).Build());
        }

        public async Task HandleAsync(MessageDeletedEventArgs args)
        {
            if (!(args.Channel is CachedTextChannel channel) ||
                !args.Message.HasValue || args.Message.Value.Author.IsBot) // TODO: What to do when message doesn't have value, other than ignore
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            var guild = await ctx.GetOrCreateGuildAsync(channel.Guild.Id);
            if (!(await ctx.GetLoggingChannelAsync(channel.Guild.Id, LogType.MessageDelete) is { } logChannel))
                return;

            var builder = new LocalEmbedBuilder()
                .WithErrorColor()
                .WithTitle(_localization.Localize(guild.Language, "logging_message_delete", channel.Tag))
                .WithDescription(args.Message.Value.Author.Format(false))
                .AddField(_localization.Localize(guild.Language, "info_id"), args.Message.Id)
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (!string.IsNullOrWhiteSpace(args.Message.Value.Content))
            {
                builder.AddField(_localization.Localize(guild.Language, "info_content"),
                    args.Message.Value.Content.TrimTo(1024, true));
            }

            if (channel.Guild.CurrentMember.Permissions.ViewAuditLog)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                var logs = await channel.Guild.GetAuditLogsAsync<RestMessagesDeletedAuditLog>(10);
                if (logs.FirstOrDefault(x =>
                    x.TargetId == args.Message.Value.Author.Id && x.ChannelId == args.Channel.Id) is { } log)
                {
                    var moderator = await args.Client.GetOrDownloadUserAsync(log.ResponsibleUserId);
                    builder.WithTitle(_localization.Localize(guild.Language, "logging_message_delete_moderator", moderator.Tag, channel.Tag));
                }
            }

            if (_temporaryImages.Remove(args.Message.Id, out var attachment))
            {
                using (attachment)
                {
                    await logChannel.SendMessageAsync(attachment,
                        embed: builder
                            .WithImageUrl($"attachment://{attachment.FileName}")
                            .Build());
                }

                return;
            }

            await logChannel.SendMessageAsync(embed: builder.Build());
        }

        public async Task HandleAsync(MessageReceivedEventArgs args)
        {
            if (!(args.Message is CachedUserMessage message) ||
                !(message.Attachments.FirstOrDefault() is { } attachment) ||
                !attachment.FileName.HasImageExtension(out _))
                return;

            // remove attachments older than 10 minutes
            // TODO: Mayyybe put this somewhere else
            foreach (var id in _temporaryImages.Keys.Where(x => DateTimeOffset.UtcNow - x.CreatedAt > TimeSpan.FromMinutes(10)))
            {
                try
                {
                    if (_temporaryImages.Remove(id, out var image))
                        image?.Dispose();
                }
                catch { /* ignored */ }
            }


            try
            {
                var stream = await _http.GetStreamAsync(attachment.Url);
                _temporaryImages.TryAdd(message.Id, new LocalAttachment(stream, attachment.FileName.Replace("SPOILER_", "spoiled_")));
            }
            catch { /* ignored */ }
        }

        public async Task HandleAsync(MemberUpdatedEventArgs args)
        {
            var oldMember = args.OldMember;
            var newMember = args.NewMember;

            using var ctx = new AdminDatabaseContext(_provider);
            var language = (await ctx.GetOrCreateGuildAsync(args.NewMember.Guild.Id)).Language;
            var builder = new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithDescription(newMember.Format(false))
                .WithTimestamp(DateTimeOffset.UtcNow);

            CachedTextChannel channel = null;

            // nickname
            if (!(newMember.Nick ?? string.Empty).Equals(oldMember.Nick ?? string.Empty,
                StringComparison.OrdinalIgnoreCase))
            {
                channel = await ctx.GetLoggingChannelAsync(oldMember.Guild.Id, LogType.NicknameUpdate);

                builder.WithTitle(_localization.Localize(language, "logging_member_nickname"))
                    .AddField(_localization.Localize(language, "logging_member_prevnick"),
                        oldMember.Nick?.Sanitize() ??
                        Markdown.Italics(_localization.Localize(language, "logging_member_noprevnick")))
                    .AddField(_localization.Localize(language, "logging_member_newnick"), 
                        newMember.Nick?.Sanitize() ??
                        Markdown.Italics(_localization.Localize(language, "logging_member_nonewnick")));
            }
            // role update
            else if (newMember.Roles.Count != oldMember.Roles.Count)
            {
                channel = await ctx.GetLoggingChannelAsync(oldMember.Guild.Id, LogType.UserRoleUpdate);
                var removedRoles = oldMember.Roles.Values.Where(x => !newMember.Roles.ContainsKey(x.Id)).ToList();
                var addedRoles = newMember.Roles.Values.Where(x => !oldMember.Roles.ContainsKey(x.Id)).ToList();

                builder.WithTitle(_localization.Localize(language, "logging_member_roleupdate"));

                if (addedRoles.Count > 0)
                    builder.AddField(_localization.Localize(language, "logging_member_addedroles"),
                        string.Join('\n', addedRoles.Select(x => x.Format(false))));

                if (removedRoles.Count > 0)
                    builder.AddField(_localization.Localize(language, "logging_member_removedroles"),
                        string.Join('\n', removedRoles.Select(x => x.Format(false))));
            }

            if (channel is { })
                await channel.SendMessageAsync(embed: builder.Build());
        }

        public async Task HandleAsync(UserUpdatedEventArgs args)
        {
            var oldUser = args.OldUser;
            var newUser = args.NewUser;

            using var ctx = new AdminDatabaseContext(_provider);
            foreach (var guild in args.Client.Guilds.Values
                .Where(x => x.Members.ContainsKey(args.NewUser.Id)))
            {
                var language = (await ctx.GetOrCreateGuildAsync(guild.Id)).Language;
                var builder = new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithDescription(newUser.Format(false))
                    .WithTimestamp(DateTimeOffset.UtcNow);

                CachedTextChannel channel = null;

                // username
                if (!newUser.Tag.Equals(oldUser.Tag, StringComparison.OrdinalIgnoreCase))
                {
                    channel = await ctx.GetLoggingChannelAsync(guild.Id, LogType.UsernameUpdate);
                    builder.WithTitle(_localization.Localize(language, "logging_member_user"))
                        .WithDescription(null)
                        .AddField(_localization.Localize(language, "logging_member_oldname"), oldUser.Tag.Sanitize())
                        .AddField(_localization.Localize(language, "logging_member_newname"), newUser.Tag.Sanitize());
                }
                // avatar
                else if (!(newUser.AvatarHash ?? string.Empty).Equals(oldUser.AvatarHash ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase))
                {
                    channel = await ctx.GetLoggingChannelAsync(guild.Id, LogType.AvatarUpdate);
                    builder.WithTitle(_localization.Localize(language, "logging_member_avatar"))
                        .WithThumbnailUrl(oldUser.GetAvatarUrl())
                        .WithImageUrl(newUser.GetAvatarUrl());
                }

                if (channel is { })
                    await channel.SendMessageAsync(embed: builder.Build());
            }
        }

        public async Task HandleAsync(ReactionRemovedEventArgs args)
        {
            if (!args.Reaction.HasValue || !(args.Channel is CachedTextChannel channel))
                return;

            var emoji = args.Reaction.Value.Emoji;

            var user = args.User.HasValue
                ? args.User.Value
                : await args.User.Downloadable.DownloadAsync() as IUser;

            if (user is null)
            {
                await _logging.LogErrorAsync($"User {args.User.Id} was null for some reason on reaction remove.",
                    "EventLogging");
                return;
            }

            if (user.IsBot)
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            if (!(await ctx.GetLoggingChannelAsync(channel.Guild.Id, LogType.ReactionRemove) is { } logChannel))
                return;

            var guild = await ctx.GetOrCreateGuildAsync(channel.Guild.Id);
            var url = EmojiTools.GetUrl(emoji);

            await logChannel.SendMessageAsync(embed: new LocalEmbedBuilder()
                .WithWarnColor()
                .WithTitle(_localization.Localize(guild.Language, "logging_reactionremove", channel.Tag))
                .WithDescription(user.Format(false))
                .AddField(_localization.Localize(guild.Language, "logging_reactionremove_emoji"),
                    $"{(emoji as Emoji)?.MessageFormat}{(emoji as CustomEmoji)?.MessageFormat}")
                .WithThumbnailUrl(url)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build());
        }
    }
}