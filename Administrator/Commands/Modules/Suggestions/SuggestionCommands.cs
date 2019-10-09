using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    [Name("Suggestions")]
    [Group("suggestions", "suggestion")]
    [RequireContext(ContextType.Guild)]
    public sealed class SuggestionCommands : AdminModuleBase
    {
        public HttpClient Http { get; set; }

        [Command("create", "add"), RunMode(RunMode.Parallel)]
        public async ValueTask<AdminCommandResult> CreateSuggestionAsync([Remainder] string text)
        {
            if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id,
                LogType.Suggestion) is { } suggestionChannel))
                return CommandErrorLocalized("suggestion_nochannel");

            Stream stream = null;
            string extension = null;
            if (Context.Message.Attachments.FirstOrDefault(x => x.Filename.HasImageExtension()) is { } image)
            {
                stream = await Http.GetStreamAsync(image.Url);
                extension = image.Filename.Split('.')[^1];
            }

            if (stream is null)
            {
                var match = StringExtensions.LazyImageLinkRegex.Match(text);
                if (match.Success)
                {
                    try
                    {
                        stream = await Http.GetStreamAsync(match.Value);
                        extension = match.Groups[2].Value;
                    }
                    catch { /* ignored */ }
                }
            }

            var suggestion = Context.Database.Suggestions.Add(new Suggestion(Context.Guild.Id, Context.User.Id, text))
                .Entity;
            await Context.Database.SaveChangesAsync();

            var builder = new EmbedBuilder()
                .WithSuccessColor()
                .WithAuthor(Context.User)
                .WithDescription(text)
                .WithFooter(Context.Localize("suggestion_id", suggestion.Id));

            RestUserMessage message;
            if (!(stream is null))
            {
                using (stream)
                {
                    message = await suggestionChannel.SendFileAsync(stream, $"image.{extension}", string.Empty,
                        embed: builder.WithImageUrl($"attachment://image.{extension}").Build());
                }
            }
            else
            {
                message = await suggestionChannel.SendMessageAsync(embed: builder.Build());
            }

            var upvote = (await Context.Database.SpecialEmotes.FindAsync(Context.Guild.Id, EmoteType.Upvote))?.Emote ??
                         EmoteTools.Upvote;
            var downvote = (await Context.Database.SpecialEmotes.FindAsync(Context.Guild.Id, EmoteType.Downvote))?.Emote ??
                EmoteTools.Downvote;

            await message.AddReactionsAsync(new[] {upvote, downvote});

            suggestion.SetMessageId(message.Id);
            Context.Database.Suggestions.Update(suggestion);
            await Context.Database.SaveChangesAsync();

            _ = Context.Message.DeleteAsync();
            return CommandSuccessLocalized("suggestion_successful", args: suggestion.Id);
        }

        [Command("remove", "rem", "rm")]
        [RequireUserPermissions(GuildPermission.ManageMessages)]
        public async ValueTask<AdminCommandResult> RemoveSuggestionAsync(int id)
        {
            var suggestion = await Context.Database.Suggestions.FindAsync(id);
            if (suggestion?.GuildId != Context.Guild.Id)
                return CommandErrorLocalized("suggestion_notfound");

            if (await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.Suggestion) is { }
                suggestionChannel)
            {
                try
                {
                    var message = await suggestionChannel.GetMessageAsync(suggestion.MessageId);
                    await message.DeleteAsync();
                }
                catch { /* ignored */ }
            }
            Context.Database.Suggestions.Remove(suggestion);
            await Context.Database.SaveChangesAsync();
            return CommandSuccessLocalized("suggestion_removed");
        }

        [Command("approve", "deny"), RunMode(RunMode.Parallel)]
        [RequireUserPermissions(GuildPermission.ManageMessages)]
        public async ValueTask<AdminCommandResult> ModifySuggestionAsync(int id, [Remainder] string reason = null)
        {
            var suggestion = await Context.Database.Suggestions.FindAsync(id);
            if (suggestion?.GuildId != Context.Guild.Id)
                return CommandErrorLocalized("suggestion_notfound");

            if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.SuggestionArchive) is { }
                archiveChannel))
                return CommandErrorLocalized("suggestion_noarchive");

            string url = null, extension = null;
            IUserMessage message = null;
            int upvotes = 0, downvotes = 0;
            if (await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.Suggestion) is { }
                suggestionChannel)
            {
                try
                {
                    message = (IUserMessage) await suggestionChannel.GetMessageAsync(suggestion.MessageId);

                    var upvote = (await Context.Database.SpecialEmotes.FindAsync(Context.Guild.Id, EmoteType.Upvote))?.Emote ??
                                 EmoteTools.Upvote;
                    var downvote = (await Context.Database.SpecialEmotes.FindAsync(Context.Guild.Id, EmoteType.Downvote))?.Emote ??
                                   EmoteTools.Downvote;

                    upvotes = Math.Max((await message.GetReactionUsersAsync(upvote, int.MaxValue).FlattenAsync()).Count() - 1,
                            0);
                    downvotes = Math.Max((await message.GetReactionUsersAsync(downvote, int.MaxValue).FlattenAsync()).Count() - 1,
                            0);

                    if (message.Attachments.FirstOrDefault() is { } image)
                    { }

                    if (message.Embeds.FirstOrDefault(x => x.Image.HasValue) is {} embed)
                    {
                        extension = embed.Image.Value.Url.Split('.')[^1];
                        url = embed.Image.Value.Url;
                    }
                }
                catch { /* ignored */ }
            }

            var author = await Context.Client.GetOrDownloadUserAsync(suggestion.UserId);
            var builder = new EmbedBuilder()
                .WithAuthor(Context.Localize(Context.Alias.Equals("approve")
                        ? "suggestion_approved_title"
                        : "suggestion_denied_title", suggestion.Id, author.ToString(), upvotes, downvotes),
                    author.GetAvatarOrDefault())
                .WithDescription(suggestion.Text)
                .WithFooter(Context.Localize(Context.Alias.Equals("approve")
                        ? "suggestion_approved_footer"
                        : "suggestion_denied_footer", Context.User.ToString()),
                    Context.User.GetAvatarOrDefault());
            if (Context.Alias.Equals("approve"))
                builder.WithSuccessColor();
            else builder.WithErrorColor();

            if (!string.IsNullOrWhiteSpace(reason))
                builder.AddField(Context.Localize("title_reason"), reason);

            if (!string.IsNullOrWhiteSpace(url))
            {
                using (var stream = await Http.GetStreamAsync(url))
                {
                    await archiveChannel.SendFileAsync(stream, $"image.{extension}", string.Empty,
                        embed: builder.WithImageUrl($"attachment://image.{extension}").Build());
                }
            }
            else
            {
                await archiveChannel.SendMessageAsync(embed: builder.Build());
            }

            _ = message?.DeleteAsync();

            Context.Database.Suggestions.Remove(suggestion);
            await Context.Database.SaveChangesAsync();
            return CommandSuccessLocalized(Context.Alias.Equals("approve") 
                ? "suggestion_approved"
                : "suggestion_denied");
        }

        [Command("channel")]
        [RequireUserPermissions(GuildPermission.ManageGuild)]
        public async ValueTask<AdminCommandResult> SetSuggestionChannel(SocketTextChannel newChannel)
        {
            if (await Context.Database.LoggingChannels.FindAsync(Context.Guild.Id, LogType.Suggestion) is { } channel)
            {
                channel.Id = newChannel.Id;
                Context.Database.LoggingChannels.Update(channel);
            }
            else
            {
                Context.Database.LoggingChannels.Add(new LoggingChannel(newChannel.Id, Context.Guild.Id,
                    LogType.Suggestion));
            }

            await Context.Database.SaveChangesAsync();
            return CommandSuccessLocalized("suggestion_channel_updated", args: newChannel.Mention);
        }

        [Command("channel")]
        [RequireUserPermissions(GuildPermission.ManageGuild)]
        public async ValueTask<AdminCommandResult> GetSuggestionChannel()
        {
            if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.Suggestion) is { } channel))
                return CommandErrorLocalized("suggestion_nochannel");

            return CommandSuccessLocalized("suggestion_channel", args: channel.Mention);
        }

        [Command("archive")]
        [RequireUserPermissions(GuildPermission.ManageGuild)]
        public async ValueTask<AdminCommandResult> SetSuggestionArchive(SocketTextChannel newChannel)
        {
            if (await Context.Database.LoggingChannels.FindAsync(Context.Guild.Id, LogType.SuggestionArchive) is { } channel)
            {
                channel.Id = newChannel.Id;
                Context.Database.LoggingChannels.Update(channel);
            }
            else
            {
                Context.Database.LoggingChannels.Add(new LoggingChannel(newChannel.Id, Context.Guild.Id,
                    LogType.SuggestionArchive));
            }

            await Context.Database.SaveChangesAsync();
            return CommandSuccessLocalized("suggestion_archive_updated", args: newChannel.Mention);
        }

        [Command("archive")]
        [RequireUserPermissions(GuildPermission.ManageGuild)]
        public async ValueTask<AdminCommandResult> GetSuggestionArchive()
        {
            if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.SuggestionArchive) is { } channel))
                return CommandErrorLocalized("suggestion_noarchive");

            return CommandSuccessLocalized("suggestion_archive", args: channel.Mention);
        }
    }
}