﻿using System.Threading.Tasks;
using Administrator.Database;
using Disqord.Bot;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands.Parsers
{
    public sealed class KeyedParser<TKeyed> : TypeParser<TKeyed>
        where TKeyed : Keyed
    {
        public override async ValueTask<TypeParserResult<TKeyed>> ParseAsync(Parameter parameter, string value, CommandContext _)
        {
            if (!int.TryParse(value, out var id))
                return Failure("The supplied value was not a number.");

            if (id < 1)
                return Failure("The supplied ID must be greater than zero.");
            
            var context = (DiscordCommandContext) _;
            await using var ctx = context.Services.GetRequiredService<AdminDbContext>();

            return await ctx.FindAsync<TKeyed>(id) is { } keyed
                ? Success(keyed)
                : Failure($"No {parameter.Name.Humanize(LetterCasing.LowerCase)} could be found with that ID.");
        }
    }
}