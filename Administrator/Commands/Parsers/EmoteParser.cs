using System;
using System.Threading.Tasks;
using Administrator.Common;
using Discord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class EmoteParser<TEmote> : TypeParser<TEmote>
        where TEmote : IEmote
    {
        public override ValueTask<TypeParserResult<TEmote>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            return EmoteTools.TryParse(value, out var result)
                ? TypeParserResult<TEmote>.Successful((TEmote) result)
                : TypeParserResult<TEmote>.Unsuccessful(context.Localize("emoteparser_notfound"));
        }
    }
}