using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class GuildParser : TypeParser<SocketGuild>
    {
        public override ValueTask<TypeParserResult<SocketGuild>> ParseAsync(Parameter parameter, string value, CommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;

            SocketGuild guild = null;

            // parse by ID
            if (ulong.TryParse(value, out var id) && id > 0)
            {
                guild = context.Client.GetGuild(id);
            }

            if (guild is null)
            {
                var matchingGuilds =
                    context.Client.Guilds.Where(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (matchingGuilds.Count > 1)
                {
                    return TypeParserResult<SocketGuild>.Unsuccessful(context.Localize("guildparser_multiple"));
                }

                guild = matchingGuilds.FirstOrDefault();
            }

            return !(guild is null)
                ? TypeParserResult<SocketGuild>.Successful(guild)
                : TypeParserResult<SocketGuild>.Unsuccessful(context.Localize("guildparser_notfound"));
        }
    }
}