using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Qmmands;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

[SlashGroup("highlight")]
public sealed class HighlightModule(HighlightService highlights) : DiscordApplicationModuleBase
{
    [SlashCommand("create")]
    [Description("Creates a new highlight for a server. If in DMs, adds a new global highlight instead.")]
    public async Task<IResult> AddAsync(
        [Description("The text you wish to be highlighted for.")] 
        [Maximum(25)]
            string text)
    {
        text = text.ToLowerInvariant();

        var result = await highlights.CreateHighlightAsync(text);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        var highlight = result.Value;
        var guild = Context.GuildId.HasValue
            ? Context.Bot.GetGuild(Context.GuildId.Value)
            : null;

        return Response(guild is not null
            ? $"{highlight.FormatKey()} New highlight created. You'll be highlighted from messages in {Markdown.Bold(guild.Name)} containing the text \"{text}\"."
            : $"{highlight.FormatKey()} New global highlight created. You'll be highlighted from messages in any mutual servers containing the text \"{text}\".")
            .AsEphemeral();
    }

    [SlashCommand("remove")]
    [Description("Deletes one of your highlights.")]
    public async Task<IResult> DeleteAsync(
        [Description("The ID of the highlight to delete.")]
            int id)
    {
        var result = await highlights.RemoveHighlightAsync(id);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        var highlight = result.Value;

        var guildName = highlight.GuildId.HasValue
            ? Bot.GetGuild(highlight.GuildId.Value) is { } guild
                ? Markdown.Bold(guild.Name)
                : Markdown.Code(highlight.GuildId.Value)
            : null;
        
        var responseBuilder = new StringBuilder("Your ")
            .Append(!highlight.GuildId.HasValue ? "global" : string.Empty)
            .Append($" highlight {highlight} for \"{highlight.Text}\"")
            .Append(guildName is not null
                ? $"in the server {guildName}"
                : string.Empty)
            .Append(" has been removed.");
        
        return Response(responseBuilder.ToString()).AsEphemeral();
    }

    [AutoComplete("remove")]
    public Task AutoCompleteHighlightsAsync(AutoComplete<int> id)
        => id.IsFocused ? highlights.AutoCompleteHighlightsAsync(id) : Task.CompletedTask;

    [SlashGroup("blacklist")]
    public sealed class HighlightBlacklistModule(AdminDbContext db, HighlightHandlingService highlights) : DiscordApplicationModuleBase
    {
        private GlobalUser _globalUser = null!;

        public override async ValueTask OnBeforeExecuted()
        {
            _globalUser = await db.GetOrCreateGlobalUserAsync(Context.AuthorId);
        }

        [SlashCommand("add")]
        [Description("Adds a user or channel to your highlight blacklist.")]
        public async Task<IResult> AddAsync(
            [Description("The user to add to your highlight blacklist.")]
            [RequireNotAuthor]
                IUser? user = null,
            [Description("The channel to add to your highlight blacklist.")]
            [ChannelTypes(ChannelType.Text, ChannelType.PrivateThread, ChannelType.PublicThread)]
            [AuthorCanViewChannel]
                IChannel? channel = null)
        {
            if (user is null && channel is null)
                return Response("You must supply a user and/or a channel to add to your blacklist.").AsEphemeral();
            
            var responseBuilder = new StringBuilder();

            if (user is not null)
            {
                responseBuilder.AppendNewline(_globalUser.BlacklistedHighlightUserIds.Add(user.Id)
                    ? $"You've added {user.Mention} to your highlight blacklist."
                    : $"You already have {user.Mention} on your blacklist!");
            }
            
            if (channel is not null)
            {
                responseBuilder.AppendNewline(_globalUser.BlacklistedHighlightChannelIds.Add(channel.Id)
                    ? $"You've added {Markdown.Bold(channel)} to your highlight blacklist."
                    : $"You already have {Markdown.Bold(channel)} on your blacklist!");
            }

            await db.SaveChangesAsync();
            highlights.InvalidateCache();

            return Response(responseBuilder.ToString()).AsEphemeral();
        }
        
        [SlashCommand("remove")]
        [Description("Removes a user or channel from your highlight blacklist.")]
        public async Task<IResult> RemoveAsync(
        [Description("The user to remove from your highlight blacklist.")]
            IUser? user = null,
        [Description("The channel to remove from your highlight blacklist.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PrivateThread, ChannelType.PublicThread)]
        [AuthorCanViewChannel]
            IChannel? channel = null)
        {
            if (user is null && channel is null)
                return Response("You must supply a user and/or a channel to remove from your blacklist.").AsEphemeral();
            
            var responseBuilder = new StringBuilder();

            if (user is not null)
            {
                responseBuilder.AppendNewline(_globalUser.BlacklistedHighlightUserIds.Remove(user.Id)
                    ? $"You've removed {user.Mention} from your highlight blacklist."
                    : $"You already do not have {user.Mention} on your blacklist!");
            }
            
            if (channel is not null)
            {
                responseBuilder.AppendNewline(_globalUser.BlacklistedHighlightChannelIds.Remove(channel.Id)
                    ? $"You've removed {Markdown.Bold(channel)} from your highlight blacklist."
                    : $"You already do not have {Markdown.Bold(channel)} on your blacklist!");
            }

            await db.SaveChangesAsync();
            highlights.InvalidateCache();

            return Response(responseBuilder.ToString()).AsEphemeral();
        }
    }

    [SlashGroup("snooze")]
    public sealed class HighlightSnoozeModule(SlashCommandMentionService mention, AdminDbContext db, HighlightHandlingService highlights) 
        : DiscordApplicationModuleBase
    {
        [SlashCommand("until")]
        [Description("Snoozes all highlights until this date/time.")]
        public async Task<IResult> SnoozeUntilAsync(
            [Description("A duration (2h30m) or instant in time (tomorrow at noon).")]
                DateTimeOffset time)
        {
            var now = Context.Interaction.CreatedAt();
            if (time < now)
            {
                return Response("You can't snooze highlights until a time in the past!\n" +
                                "(If this time isn't in the past for you, try changing your timezone with " +
                                $"{mention.GetMention("self timezone")}.)").AsEphemeral();
            }

            var user = await db.GetOrCreateGlobalUserAsync(Context.AuthorId);
            user.HighlightsSnoozedUntil = time;
            await db.SaveChangesAsync();
            highlights.InvalidateCache();

            return Response($"All highlights have been snoozed until {Markdown.Timestamp(time, Markdown.TimestampFormat.LongDateTime)}").AsEphemeral();
        }

        [SlashCommand("cancel")]
        [Description("Cancels any current highlight snoozing.")]
        public async Task<IResult> CancelSnoozeAsync()
        {
            var user = await db.GetOrCreateGlobalUserAsync(Context.AuthorId);
            user.HighlightsSnoozedUntil = null;
            await db.SaveChangesAsync();
            highlights.InvalidateCache();

            return Response("You will now be highlighted again (snoozing canceled).").AsEphemeral();
        }
    }
}