using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class GuildParser : TypeParser<CachedGuild>
    {
        public override ValueTask<TypeParserResult<CachedGuild>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;

            CachedGuild guild = null;

            // parse by ID
            if (ulong.TryParse(value, out var id) && id > 0)
            {
                guild = context.Client.GetGuild(id);
            }

            if (guild is null)
            {
                var matchingGuilds =
                    context.Client.Guilds.Values.Where(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (matchingGuilds.Count > 1)
                {
                    return TypeParserResult<CachedGuild>.Unsuccessful(context.Localize("guildparser_multiple"));
                }

                guild = matchingGuilds.FirstOrDefault();
            }

            return !(guild is null)
                ? TypeParserResult<CachedGuild>.Successful(guild)
                : TypeParserResult<CachedGuild>.Unsuccessful(context.Localize("guildparser_notfound"));
        }
    }
}