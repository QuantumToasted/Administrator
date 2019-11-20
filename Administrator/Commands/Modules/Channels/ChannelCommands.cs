using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Administrator.Commands
{
    [Name("Channels")]
    [Group("channel")]
    [RequireContext(ContextType.Guild)]
    public class ChannelCommands : AdminModuleBase
    {
        [Command("", "info")]
        public AdminCommandResult GetChannelInfo([Remainder] SocketGuildChannel channel = null)
        {
            channel ??= (SocketTextChannel) Context.Channel;

            var builder = new EmbedBuilder()
                .WithSuccessColor()
                .AddField(Localize("info_id"), channel.Id)
                .AddField(Localize("info_created"), string.Join('\n', channel.CreatedAt.ToString("g", Context.Language.Culture),
                (DateTimeOffset.UtcNow - channel.CreatedAt).HumanizeFormatted(Context, TimeUnit.Minute, true)));

            builder = channel switch
            {
                SocketNewsChannel newsChannel => builder
                    .WithTitle(Localize("channel_info_news", $"#{newsChannel.Name}"))
                    .WithDescription(Format.Italics(newsChannel.Topic))
                    .AddField(Localize("info_mention"), newsChannel.Mention)
                    .WithFooter(newsChannel.Category is { } 
                        ? Localize("channel_info_category_footer", newsChannel.Category.Name.Sanitize()) 
                        : null),
                SocketTextChannel textChannel => builder
                    .WithTitle(Localize("channel_info_text", $"#{textChannel.Name}"))
                    .WithDescription(Format.Italics(textChannel.Topic))
                    .AddField(Localize("info_mention"), textChannel.Mention)
                    .WithFooter(textChannel.Category is { }
                        ? Localize("channel_info_category_footer", textChannel.Category.Name.Sanitize())
                        : null),
                SocketVoiceChannel voiceChannel => builder
                    .WithTitle(Localize("channel_info_voice", voiceChannel.Name.Sanitize()))
                    .AddField(Localize("channel_info_voice_bitrate"), $"{voiceChannel.Bitrate/1000}kbps")
                    .AddField(Localize("channel_info_voice_connected",
                        $"{voiceChannel.Users.Count}/{voiceChannel.UserLimit?.ToString() ?? "∞"}"), 
                        voiceChannel.Users.Count > 0 
                            ? string.Join(", ", voiceChannel.Users.Select(x => x.ToString().Sanitize())).TrimTo(1024, true) 
                            : Localize("info_none"))
                    .WithFooter(voiceChannel.Category is { }
                        ? Localize("channel_info_category_footer", voiceChannel.Category.Name.Sanitize())
                        : null),
                SocketCategoryChannel category => builder
                    .WithTitle(Localize("channel_info_category", category.Name.Sanitize()))
                    .AddField(Localize("channel_info_category_channels"), category.Channels.Count > 0 
                        ? string.Join('\n', category.Channels.OrderBy(x => x.Position).Select(x => x.Format())) 
                        : Localize("info_none")),
                _ => throw new ArgumentOutOfRangeException(nameof(channel))
            };

            return CommandSuccess(embed: builder.Build());
        }

        [RequireBotPermissions(GuildPermission.ManageChannels)]
        [RequireUserPermissions(GuildPermission.ManageChannels)]
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
                            throw new ArgumentOutOfRangeException("Can't create a news channel. Yet!");
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
            public async ValueTask<AdminCommandResult> RenameChannelAsync(SocketGuildChannel channel, [Remainder] string newName)
            {
                await channel.ModifyAsync(x => x.Name = newName);
                return CommandSuccessLocalized("channel_rename");
            }

            [Command("delete")]
            public async ValueTask<AdminCommandResult> DeleteChannelAsync([Remainder] SocketGuildChannel channel)
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
                    Format.Code(string.Join('\n', loggingChannels.Select(x => x.Type.ToString())), "")));
            }

            [Command("logevent")]
            public async ValueTask<AdminCommandResult> ToggleLogEventsAsync(LogType logType)
            {
                if (logType == LogType.Modmail)
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
