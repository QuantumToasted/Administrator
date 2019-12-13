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

namespace Administrator.Services
{
    public sealed class EventLoggingService : IService,
        IHandler<MemberLeftEventArgs>,
        IHandler<MemberJoinedEventArgs>,
        IHandler<MessageDeletedEventArgs>,
        IHandler<MessageUpdatedEventArgs>,
        IHandler<MessageReceivedEventArgs>
    {
        private readonly IServiceProvider _provider;
        private readonly HttpClient _http;
        private readonly LocalizationService _localization;
        private readonly LoggingService _logging;
        private readonly Dictionary<Snowflake, LocalAttachment> _temporaryImages;

        public EventLoggingService(IServiceProvider provider, HttpClient http, LocalizationService localization,
            LoggingService logging)
        {
            _provider = provider;
            _http = http;
            _localization = localization;
            _logging = logging;
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

        Task IService.InitializeAsync()
            => _logging.LogInfoAsync("Initialized.", "EventLogging");
    }
}