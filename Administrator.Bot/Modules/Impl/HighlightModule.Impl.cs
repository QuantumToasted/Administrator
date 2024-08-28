using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

public enum ViewMode
{
    User,
    Channel
}

public sealed partial class HighlightModule(HighlightService highlights, AdminDbContext db, HighlightHandlingService highlightHandling) : DiscordApplicationModuleBase
{
    public partial async Task Clear()
    {
        var userHighlights = db.Highlights.Where(x => x.AuthorId == Context.AuthorId);
        var contextHighlights = await (Context.GuildId.HasValue
            ? userHighlights.Where(x => x.GuildId == Context.GuildId.Value)
            : userHighlights.Where(x => x.GuildId == null)).OrderByDescending(x => x.Id).ToListAsync();

        if (contextHighlights.Count == 0)
        {
            await Response(Context.GuildId.HasValue
                ? $"You don't have any highlights in {Markdown.Bold(Bot.GetGuild(Context.GuildId.Value)!.Name)}!"
                : "You don't have any global highlights!").AsEphemeral(Context.GuildId.HasValue);

            return;
        }

        var view = new AdminPromptView(
                $"{$"{(!Context.GuildId.HasValue ? "global" : "server")} highlight".ToQuantity(contextHighlights.Count)} will be cleared.\n\n" +
                Markdown.Bold("This action CANNOT be undone."))
            .OnConfirm("Highlights cleared.");

        await View(view);

        if (view.Result)
        {
            db.Highlights.RemoveRange(contextHighlights);
            await db.SaveChangesAsync();
            highlightHandling.InvalidateCache();
        }
    }
    
    public partial async Task<IResult> List()
    {
        var userHighlights = db.Highlights.Where(x => x.AuthorId == Context.AuthorId);
        var contextHighlights = await (Context.GuildId.HasValue
            ? userHighlights.Where(x => x.GuildId == Context.GuildId.Value)
            : userHighlights.Where(x => x.GuildId == null)).OrderByDescending(x => x.Id).ToListAsync();

        if (contextHighlights.Count == 0)
        {
            return Response(Context.GuildId.HasValue
                ? $"You don't have any highlights in {Markdown.Bold(Bot.GetGuild(Context.GuildId.Value)!.Name)}!"
                : "You don't have any global highlights!").AsEphemeral(Context.GuildId.HasValue);
        }

        var pages = new List<Page>();
        var descriptionBuilder = new StringBuilder();
        foreach (var highlight in contextHighlights)
        {
            var format = $"{highlight} - {highlight.Text}\n";
            if (descriptionBuilder.Length + format.Length >= Discord.Limits.Message.Embed.MaxDescriptionLength)
            {
                pages.Add(GeneratePage(descriptionBuilder, Context.GuildId.HasValue ? Bot.GetGuild(Context.GuildId.Value) : null));
                descriptionBuilder.Clear();
            }

            descriptionBuilder.AppendNewline(format);
        }

        if (descriptionBuilder.Length > 0)
        {
            pages.Add(GeneratePage(descriptionBuilder, Context.GuildId.HasValue ? Bot.GetGuild(Context.GuildId.Value) : null));
        }

        if (pages.Count == 1)
        {
            var firstPage = pages[0];
            return Response(new LocalInteractionMessageResponse { Content = firstPage.Content, Embeds = firstPage.Embeds }).AsEphemeral(Context.GuildId.HasValue);
        }

        return Menu(new AdminInteractionMenu(new AdminPagedView(pages, Context.GuildId.HasValue), Context.Interaction));

        static Page GeneratePage(StringBuilder sb, IGuild? guild)
        {
            return new Page().WithContent(guild is not null
                    ? $"Your highlights in {Markdown.Bold(guild.Name)}:"
                    : "Your global highlights:")
                .AddEmbed(new LocalEmbed()
                    .WithUnusualColor()
                    .WithDescription(sb.ToString()));
        }
    }
    
    public partial async Task<IResult> Add(string text)
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
            ? $"{highlight} New highlight created. You'll be highlighted from messages in {Markdown.Bold(guild.Name)} containing the text \"{text}\"."
            : $"{highlight} New global highlight created. You'll be highlighted from messages in any mutual servers containing the text \"{text}\".")
            .AsEphemeral(Context.GuildId.HasValue);
    }
    
    public partial async Task<IResult> Delete(int id)
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
        
        return Response(responseBuilder.ToString()).AsEphemeral(Context.GuildId.HasValue);
    }
    
    public partial Task AutoCompleteHighlights(AutoComplete<int> id)
        => id.IsFocused ? highlights.AutoCompleteHighlightsAsync(id) : Task.CompletedTask;

    public sealed partial class HighlightBlacklistModule(AdminDbContext db, HighlightHandlingService highlights) : DiscordApplicationModuleBase
    {
        private User _user = null!;

        public override async ValueTask OnBeforeExecuted()
        {
            _user = await db.Users.GetOrCreateAsync(Context.AuthorId);
        }
        
        public partial async Task<IResult> View(ViewMode mode)
        {
            var blacklist = mode == ViewMode.Channel
                ? _user.BlacklistedHighlightChannelIds
                : _user.BlacklistedHighlightUserIds;

            var formatted = new List<string>();
            foreach (var id in blacklist)
            {
                if (mode == ViewMode.User)
                {
                    formatted.Add(await Bot.GetOrFetchUserAsync(id) is { } user 
                        ? $"{user.Format()}"
                        : Markdown.Code(id));
                }
                else
                {
                    formatted.Add(Bot.TryGetAnyGuildChannel(id, out var channel) && Bot.GetGuild(channel.GuildId) is { } guild
                        ? $"{Markdown.Bold($"#{channel.Name}")} ({Markdown.Code(id)}) in {Markdown.Bold(guild.Name)}"
                        : Markdown.Code(id));
                }
            }

            var pages = formatted.Chunk(25)
                .Select(x => new Page()
                    .AddEmbed(new LocalEmbed()
                        .WithUnusualColor()
                        .WithTitle(mode == ViewMode.User
                            ? "Blacklisted users"
                            : "Blacklisted channels")
                        .WithDescription(string.Join('\n', x))))
                .ToList();
            
            return pages.Count switch
            {
                0 => Response($"Your {mode.ToString().ToLower()} highlight blacklist is empty.").AsEphemeral(),
                1 => Response(pages[0].Embeds.Value[0]).AsEphemeral(),
                _ => Menu(new AdminInteractionMenu(new AdminPagedView(pages, true), Context.Interaction))
            };
        }
        
        public partial async Task<IResult> Add(IUser? user, IChannel? channel)
        {
            if (user is null && channel is null)
                return Response("You must supply a user and/or a channel to add to your blacklist.").AsEphemeral();
            
            var responseBuilder = new StringBuilder();

            if (user is not null)
            {
                responseBuilder.AppendNewline(_user.BlacklistedHighlightUserIds.TryAddUnique(user.Id)
                    ? $"You've added {user.Mention} to your highlight blacklist."
                    : $"You already have {user.Mention} on your blacklist!");
            }
            
            if (channel is not null)
            {
                responseBuilder.AppendNewline(_user.BlacklistedHighlightChannelIds.TryAddUnique(channel.Id)
                    ? $"You've added {Markdown.Bold(channel)} to your highlight blacklist."
                    : $"You already have {Markdown.Bold(channel)} on your blacklist!");
            }

            await db.SaveChangesAsync();
            highlights.InvalidateCache();

            return Response(responseBuilder.ToString()).AsEphemeral(Context.GuildId.HasValue);
        }

        public partial async Task<IResult> Remove(IUser? user, IChannel? channel)
        {
            if (user is null && channel is null)
                return Response("You must supply a user and/or a channel to remove from your blacklist.").AsEphemeral();
            
            var responseBuilder = new StringBuilder();

            if (user is not null)
            {
                responseBuilder.AppendNewline(_user.BlacklistedHighlightUserIds.Remove(user.Id)
                    ? $"You've removed {user.Mention} from your highlight blacklist."
                    : $"You already do not have {user.Mention} on your blacklist!");
            }
            
            if (channel is not null)
            {
                responseBuilder.AppendNewline(_user.BlacklistedHighlightChannelIds.Remove(channel.Id)
                    ? $"You've removed {Markdown.Bold(channel)} from your highlight blacklist."
                    : $"You already do not have {Markdown.Bold(channel)} on your blacklist!");
            }

            await db.SaveChangesAsync();
            highlights.InvalidateCache();

            return Response(responseBuilder.ToString()).AsEphemeral(Context.GuildId.HasValue);
        }
    }

    public sealed partial class HighlightSnoozeModule(SlashCommandMentionService mention, AdminDbContext db, HighlightHandlingService highlights) 
        : DiscordApplicationModuleBase
    {
        public partial async Task<IResult> Until(DateTimeOffset time)
        {
            var now = Context.Interaction.CreatedAt();
            if (time < now)
            {
                return Response("You can't snooze highlights until a time in the past!\n" +
                                "(If this time isn't in the past for you, try changing your timezone with " +
                                $"{mention.GetMention("self timezone")}.)").AsEphemeral();
            }

            var user = await db.Users.GetOrCreateAsync(Context.AuthorId);
            user.HighlightsSnoozedUntil = time;
            await db.SaveChangesAsync();
            highlights.InvalidateCache();

            return Response($"All highlights have been snoozed until {Markdown.Timestamp(time, Markdown.TimestampFormat.LongDateTime)}.")
                .AsEphemeral(Context.GuildId.HasValue);
        }

        public partial async Task<IResult> Cancel()
        {
            var user = await db.Users.GetOrCreateAsync(Context.AuthorId);
            user.HighlightsSnoozedUntil = null;
            await db.SaveChangesAsync();
            highlights.InvalidateCache();

            return Response("You will now be highlighted again (snoozing canceled).").AsEphemeral(Context.GuildId.HasValue);
        }
    }
}