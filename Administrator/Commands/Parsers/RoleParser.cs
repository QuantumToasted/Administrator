using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class RoleParser<TRole> : TypeParser<TRole>
        where TRole : SocketRole
    {
        public override ValueTask<TypeParserResult<TRole>> ParseAsync(Parameter parameter, string value, CommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            if (context.IsPrivate)
                return TypeParserResult<TRole>.Unsuccessful("requirecontext_guild");

            TRole role = null;

            // Parse by ID or mention
            if (ulong.TryParse(value, out var id) || MentionUtils.TryParseRole(value, out id))
            {
                role = context.Guild.GetRole(id) as TRole;
            }

            // Parse by name
            if (role is null)
            {
                var matches = context.Guild.Roles.Where(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count > 1)
                    return TypeParserResult<TRole>.Unsuccessful(context.Localize("roleparser_multiple"));

                role = matches.FirstOrDefault() as TRole;
            }

            return !(role is null)
                ? TypeParserResult<TRole>.Successful(role)
                : TypeParserResult<TRole>.Unsuccessful(context.Localize("roleparser_notfound"));
        }
    }
}