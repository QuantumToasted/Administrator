using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class RoleParser : TypeParser<CachedRole>
    {
        public override ValueTask<TypeParserResult<CachedRole>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            if (context.IsPrivate)
                return TypeParserResult<CachedRole>.Unsuccessful(context.Localize("requirecontext_guild"));

            CachedRole role = null;

            // Parse by ID or mention
            if (Snowflake.TryParse(value, out var id) || Discord.TryParseRoleMention(value, out id))
            {
                role = context.Guild.GetRole(id);
            }

            // Parse by name
            if (role is null)
            {
                var matches = context.Guild.Roles.Values.Where(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count > 1)
                    return TypeParserResult<CachedRole>.Unsuccessful(context.Localize("roleparser_multiple"));

                role = matches.FirstOrDefault();
            }

            return !(role is null)
                ? TypeParserResult<CachedRole>.Successful(role)
                : TypeParserResult<CachedRole>.Unsuccessful(context.Localize("roleparser_notfound"));
        }
    }
}