using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
using Permission = Disqord.Permission;

namespace Administrator.Commands
{
    [Name("Channels")]
    [Group("channel")]
    [RequireContext(ContextType.Guild)]
    public class ChannelCommands : AdminModuleBase
    {
        [Command("", "info")]
        public AdminCommandResult GetChannelInfo([Remainder] CachedGuildChannel channel = null)
        {
            channel ??= (CachedTextChannel) Context.Channel;

            var builder = new LocalEmbedBuilder()
                .WithSuccessColor()
                .AddField(Localize("info_id"), channel.Id)
                .AddField(Localize("info_created"), string.Join('\n', channel.Id.CreatedAt.ToString("g", Context.Language.Culture),
                (DateTimeOffset.UtcNow - channel.Id.CreatedAt).HumanizeFormatted(Context, TimeUnit.Minute, true)));

            builder = channel switch
            {
                CachedTextChannel textChannel => builder
                    .WithTitle(Localize(textChannel.IsNews ? "channel_info_news" : "channel_info_text", $"#{textChannel.Name}"))
                    .WithDescription(Markdown.Italics(textChannel.Topic))
                    .AddField(Localize("info_mention"), textChannel.Mention)
                    .WithFooter(textChannel.Category is { }
                        ? Localize("channel_info_category_footer", textChannel.Category.Name.Sanitize())
                        : null),
                CachedVoiceChannel voiceChannel => builder
                    .WithTitle(Localize("channel_info_voice", voiceChannel.Name.Sanitize()))
                    .AddField(Localize("channel_info_voice_bitrate"), $"{voiceChannel.Bitrate/1000}kbps")
                    .AddField(Localize("channel_info_voice_connected",
                        $"{voiceChannel.Members.Count}/{(voiceChannel.UserLimit == 0 ? "∞" : voiceChannel.UserLimit.ToString())}"),
                        voiceChannel.Members.Count > 0 
                            ? string.Join(", ", voiceChannel.Members.Values.Select(x => x.ToString().Sanitize())).TrimTo(1024, true) 
                            : Localize("info_none"))
                    .WithFooter(voiceChannel.Category is { }
                        ? Localize("channel_info_category_footer", voiceChannel.Category.Name.Sanitize())
                        : null),
                CachedCategoryChannel category => builder
                    .WithTitle(Localize("channel_info_category", category.Name.Sanitize()))
                    .AddField(Localize("channel_info_category_channels"), category.Channels.Count > 0 
                        ? string.Join('\n', category.Channels.Values.OrderBy(x => x.Position).Select(x => x.Format())) 
                        : Localize("info_none")),
                _ => throw new ArgumentOutOfRangeException(nameof(channel))
            };

            return CommandSuccess(embed: builder.Build());
        }

        [RequireBotPermissions(Permission.ManageChannels)]
        [RequireUserPermissions(Permission.ManageChannels)]
        public class ChannelManagementCommands : ChannelCommands
        {
            [Group("create")]
            public sealed class ChannelCreationCommands : ChannelManagementCommands
            {
                [Command("text", "news", "voice", "category")]
                public async ValueTask<AdminCommandResult> CreateChannelAsync(
                    [Remainder, MustBe(Operator.LessThan, 32)] string name)
                {
                    RestGuildChannel channel;
                    switch (Context.Path[2])
                    {
                        case "text":
                            channel = await Context.Guild.CreateTextChannelAsync(name);
                            break;
                        case "news":
                            // TODO: CreateNewsChannel?
                            throw new ArgumentOutOfRangeException();
                        // break;
                        case "voice":
                            channel = await Context.Guild.CreateVoiceChannelAsync(name);
                            break;
                        case "category":
                            channel = await Context.Guild.CreateCategoryChannelAsync(name);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    return CommandSuccessLocalized($"channel_create_{Context.Path[2]}", args: channel.Format());
                }
            }

            [Command("rename")]
            public async ValueTask<AdminCommandResult> RenameChannelAsync(CachedGuildChannel channel, 
                [Remainder, MustBe(Operator.LessThan, 32)] string newName)
            {
                switch (channel)
                {
                    case CachedCategoryChannel category:
                        await category.ModifyAsync(x => x.Name = newName);
                        break;
                    case CachedTextChannel textChannel:
                        await textChannel.ModifyAsync(x => x.Name = newName);
                        break;
                    case CachedVoiceChannel voiceChannel:
                        await voiceChannel.ModifyAsync(x => x.Name = newName);
                        break;
                }

                return CommandSuccessLocalized("channel_rename");
            }

            [Command("delete")]
            public async ValueTask<AdminCommandResult> DeleteChannelAsync([Remainder] CachedGuildChannel channel)
            {
                await channel.DeleteAsync();
                return CommandSuccessLocalized("channel_delete");
            }

            [Command("logevents")]
            public async ValueTask<AdminCommandResult> ViewLogEventsAsync()
            {
                var loggingChannels = await Context.Database.LoggingChannels.Where(x => x.Id == Context.Channel.Id)
                    .ToListAsync();

                if (loggingChannels.Count == 0)
                    return CommandErrorLocalized("channel_logging_none");

                return CommandSuccess(string.Join('\n', Localize("channel_logging_list"), 
                    Markdown.CodeBlock(string.Join('\n', loggingChannels.Select(x => x.Type.ToString())))));
            }

            [Command("logevent")]
            public async ValueTask<AdminCommandResult> ToggleLogEventsAsync(LogType logType)
            {
                if (logType == LogType.Modmail || logType == LogType.Suggestion || logType == LogType.SuggestionArchive)
                {
                    // fail silently
                    return CommandSuccess();
                }

                if (logType == LogType.Disable)
                {
                    var channels = await Context.Database.LoggingChannels.Where(x => x.Id == Context.Channel.Id)
                        .ToListAsync();

                    if (channels.Count > 0)
                    {
                        Context.Database.LoggingChannels.RemoveRange(channels);
                        await Context.Database.SaveChangesAsync();
                    }

                    return CommandSuccessLocalized("channel_logging_alldisabled");
                }

                if (await Context.Database.LoggingChannels.FindAsync(Context.Guild.Id, logType) is { } loggingChannel)
                {
                    loggingChannel.Id = Context.Channel.Id;
                    Context.Database.LoggingChannels.Update(loggingChannel);
                }
                else
                {
                    Context.Database.LoggingChannels.Add(new LoggingChannel(Context.Channel.Id, Context.Guild.Id, logType));
                }

                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized($"channel_logging_{logType.ToString().ToLower()}");
            }
        }
    }
}
