using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class UserParser<TUser> : TypeParser<TUser>
        where TUser : CachedUser
    {
        public override ValueTask<TypeParserResult<TUser>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            if (context.IsPrivate)
                return TypeParserResult<TUser>.Unsuccessful(context.Localize("requirecontext_guild"));

            TUser user = null;

            // Parse by ID or mention
            if (Snowflake.TryParse(value, out var id) || Discord.TryParseUserMention(value, out id))
            {
                user = context.Guild.GetMember(id) as TUser;
            }

            // Parse by user#discrim
            if (user is null)
            {
                user = context.Guild.Members.Values.FirstOrDefault(x =>
                    x.Tag.Equals(value, StringComparison.OrdinalIgnoreCase)) as TUser;
            }

            // Parse by exact username/nickname match
            if (user is null)
            {
                var matches = context.Guild.Members.Values.Where(x =>
                        x.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true ||
                        x.Nick?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                if (matches.Count > 1)
                    return TypeParserResult<TUser>.Unsuccessful(context.Localize("userparser_multiple"));

                user = matches.FirstOrDefault() as TUser;
            }

            return !(user is null)
                ? TypeParserResult<TUser>.Successful(user)
                : TypeParserResult<TUser>.Unsuccessful(context.Localize("userparser_notfound"));
        }
    }
}