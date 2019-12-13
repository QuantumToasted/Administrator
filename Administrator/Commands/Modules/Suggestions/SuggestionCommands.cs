using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.Rest;
using Qmmands;
using Permission = Disqord.Permission;

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

            var image = new MemoryStream();
            var format = ImageFormat.Default;
            if (Context.Message.Attachments.FirstOrDefault() is { } attachment &&
                attachment.FileName.HasImageExtension(out format))
            {
                await using var stream = await Http.GetStreamAsync(attachment.Url);
                await stream.CopyToAsync(image);
                image.Seek(0, SeekOrigin.Begin);
            }

            if (format == ImageFormat.Default)
            {
                var match = StringExtensions.LazyImageLinkRegex.Match(text);
                if (match.Success && match.Value.HasImageExtension(out format))
                {
                    try
                    {
                        await using var stream = await Http.GetStreamAsync(match.Value);
                        await stream.CopyToAsync(image);
                        image.Seek(0, SeekOrigin.Begin);
                    }
                    catch { /* ignored */ }
                }
            }

            var suggestion = Context.Database.Suggestions.Add(new Suggestion(Context.Guild.Id, Context.User.Id, text, image, format))
                .Entity;
            await Context.Database.SaveChangesAsync();

            var builder = new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithAuthor(Context.User)
                .WithDescription(text)
                .WithFooter(Context.Localize("suggestion_id", suggestion.Id));

            RestUserMessage message;
            if (format != ImageFormat.Default)
            {
                message = await suggestionChannel.SendMessageAsync(new LocalAttachment(image, $"attachment.{format.ToString().ToLower()}"),
                    embed: builder.WithImageUrl($"attachment://attachment.{format.ToString().ToLower()}").Build());
            }
            else
            {
                message = await suggestionChannel.SendMessageAsync(embed: builder.Build());
            }

            var upvote = (await Context.Database.SpecialEmojis.FindAsync(Context.Guild.Id.RawValue, EmojiType.Upvote))?.Emoji ??
                         EmojiTools.Upvote;
            var downvote = (await Context.Database.SpecialEmojis.FindAsync(Context.Guild.Id.RawValue, EmojiType.Downvote))?.Emoji ??
                EmojiTools.Downvote;

            await message.AddReactionAsync(upvote);
            await message.AddReactionAsync(downvote);

            suggestion.SetMessageId(message.Id);
            Context.Database.Suggestions.Update(suggestion);
            await Context.Database.SaveChangesAsync();

            _ = Context.Message.DeleteAsync();
            return CommandSuccessLocalized("suggestion_successful", args: suggestion.Id);
        }

        [Command("remove")]
        [RequireUserPermissions(Permission.ManageMessages)]
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
        [RequireUserPermissions(Permission.ManageMessages)]
        public async ValueTask<AdminCommandResult> ModifySuggestionAsync(int id, [Remainder] string reason = null)
        {
            var suggestion = await Context.Database.Suggestions.FindAsync(id);
            if (suggestion?.GuildId != Context.Guild.Id)
                return CommandErrorLocalized("suggestion_notfound");

            if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.SuggestionArchive) is { }
                archiveChannel))
                return CommandErrorLocalized("suggestion_noarchive");

            IUserMessage message = null;
            int upvotes = 0, downvotes = 0;
            if (await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.Suggestion) is { }
                suggestionChannel)
            {
                try
                {
                    message = (IUserMessage) await suggestionChannel.GetMessageAsync(suggestion.MessageId);

                    var upvote = (await Context.Database.SpecialEmojis.FindAsync(Context.Guild.Id.RawValue, EmojiType.Upvote))?.Emoji ??
                                 EmojiTools.Upvote;
                    var downvote = (await Context.Database.SpecialEmojis.FindAsync(Context.Guild.Id.RawValue, EmojiType.Downvote))?.Emoji ??
                                   EmojiTools.Downvote;

                    upvotes = Math.Max((await message.GetReactionsAsync(upvote, int.MaxValue)).Count - 1, 0);
                    downvotes = Math.Max((await message.GetReactionsAsync(downvote, int.MaxValue)).Count - 1, 0);
                }
                catch { /* ignored */ }
            }

            var author = await Context.Client.GetOrDownloadUserAsync(suggestion.UserId);
            var builder = new LocalEmbedBuilder()
                .WithAuthor(Context.Localize(Context.Alias.Equals("approve")
                        ? "suggestion_approved_title"
                        : "suggestion_denied_title", suggestion.Id, author.Tag, upvotes, downvotes),
                    author.GetAvatarUrl())
                .WithDescription(suggestion.Text)
                .WithFooter(Context.Localize(Context.Alias.Equals("approve")
                        ? "suggestion_approved_footer"
                        : "suggestion_denied_footer", Context.User.Tag),
                    Context.User.GetAvatarUrl());
            if (Context.Alias.Equals("approve"))
                builder.WithSuccessColor();
            else builder.WithErrorColor();

            if (!string.IsNullOrWhiteSpace(reason))
                builder.AddField(Context.Localize("title_reason"), reason);

            if (suggestion.Format != ImageFormat.Default)
            {
                await archiveChannel.SendMessageAsync(new LocalAttachment(suggestion.Image, $"attachment.{suggestion.Format.ToString().ToLower()}"),
                    embed: builder.WithImageUrl($"attachment://attachment.{suggestion.Format.ToString().ToLower()}").Build());
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
        [RequireUserPermissions(Permission.ManageGuild)]
        public async ValueTask<AdminCommandResult> SetSuggestionChannel(CachedTextChannel newChannel)
        {
            if (await Context.Database.LoggingChannels.FindAsync(Context.Guild.Id.RawValue, LogType.Suggestion) is { } channel)
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
        [RequireUserPermissions(Permission.ManageGuild)]
        public async ValueTask<AdminCommandResult> GetSuggestionChannel()
        {
            if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.Suggestion) is { } channel))
                return CommandErrorLocalized("suggestion_nochannel");

            return CommandSuccessLocalized("suggestion_channel", args: channel.Mention);
        }

        [Command("archive")]
        [RequireUserPermissions(Permission.ManageGuild)]
        public async ValueTask<AdminCommandResult> SetSuggestionArchive(CachedTextChannel newChannel)
        {
            if (await Context.Database.LoggingChannels.FindAsync(Context.Guild.Id.RawValue, LogType.SuggestionArchive) is { } channel)
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
        [RequireUserPermissions(Permission.ManageGuild)]
        public async ValueTask<AdminCommandResult> GetSuggestionArchive()
        {
            if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, LogType.SuggestionArchive) is { } channel))
                return CommandErrorLocalized("suggestion_noarchive");

            return CommandSuccessLocalized("suggestion_archive", args: channel.Mention);
        }
    }
}