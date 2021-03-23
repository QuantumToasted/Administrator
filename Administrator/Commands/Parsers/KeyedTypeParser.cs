using System.Threading.Tasks;
using Administrator.Database;
using Disqord.Bot;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class KeyedTypeParser<TKeyed> : DiscordTypeParser<TKeyed>
        where TKeyed : Keyed
    {
        public override async ValueTask<TypeParserResult<TKeyed>> ParseAsync(Parameter parameter, string value, DiscordCommandContext context)
        {
            if (!int.TryParse(value, out var id))
                return Failure("The supplied value was not a number.");

            if (id < 1)
                return Failure("The supplied ID must be greater than zero.");
            
            await using var ctx = context.Services.GetRequiredService<AdminDbContext>();

            return await ctx.FindAsync<TKeyed>(id) is { } keyed
                ? Success(keyed)
                : Failure($"No {parameter.Name.Humanize(LetterCasing.LowerCase)} could be found with that ID.");
        }
    }
}