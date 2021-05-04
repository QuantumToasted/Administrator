using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Disqord.Bot;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class HighlightTypeParser : KeyedTypeParser<Highlight>
    {
        public override async ValueTask<TypeParserResult<Highlight>> ParseAsync(Parameter parameter, string value, DiscordCommandContext context)
        {
            using var scope = context.Bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            var guild = (context as DiscordGuildCommandContext)?.Guild;

            // TODO: What if a user sets a number as their highlight?
            var baseParseResult = await base.ParseAsync(parameter, value, context);
            if (baseParseResult.IsSuccessful)
                return Success(baseParseResult.Value);

            var highlights = await ctx.GetHighlightsAsync();
            if (highlights.FirstOrDefault(x =>
                x.UserId == context.Author.Id && x.GuildId == context.GuildId &&
                x.Text.Equals(value, StringComparison.InvariantCultureIgnoreCase)) is { } highlight)
            {
                return Success(highlight);
            }

            return Failure(guild is not null
                ? $"You don't have a highlight for that text in {guild.Name.Sanitize()}!"
                : "You don't have a global highlight for that text!");
        }
    }
}