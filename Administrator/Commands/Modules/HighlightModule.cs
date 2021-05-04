using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Disqord.Bot;
using Disqord.Gateway;
using Qmmands;

namespace Administrator.Commands
{
    [Name("Highlights")]
    [Group("highlights", "highlight", "hl")]
    public sealed class HighlightModule : DiscordModuleBase
    {
        public AdminDbContext Database { get; set; }
        
        [CreateCommand]
        public async Task<DiscordCommandResult> CreateHighlightAsync([Remainder, Lowercase, Maximum(32)] string text)
        {
            var highlights = await Database.GetHighlightsAsync();
            var guild = (Context as DiscordGuildCommandContext)?.Guild;

            if (highlights.Any(x =>
                x.UserId == Context.Author.Id && x.GuildId == Context.GuildId &&
                x.Text.Equals(text, StringComparison.InvariantCultureIgnoreCase)))
            {
                return Response(guild is not null
                    ? $"You already have a highlight for this text in {guild.Name.Sanitize()}!"
                    : "You already have a global highlight for this text!");
            }

            var highlight = Database.Highlights.Add(Highlight.Create(Context.Author, guild, text)).Entity;
            await Database.SaveChangesAsync();

            return Response((guild is not null
                                ? $"{highlight} New highlight created for {guild.Name.Sanitize()}.\n"
                                : $"{highlight} New global highlight created.\n") +
                            "I will DM you whenever someone mentions the following text in channels you can see:\n" +
                            $"\"{text}\"");
        }

        [DeleteCommand]
        public async Task<DiscordCommandResult> DeleteHighlightAsync([Remainder, SameUser] Highlight highlight)
        {
            Database.Remove(highlight);
            await Database.SaveChangesAsync();

            return Response($"Highlight {highlight} successfully removed.");
        }
    }
}