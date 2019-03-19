using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class UserParser<TUser> : TypeParser<TUser>
        where TUser : SocketUser
    {
        public override ValueTask<TypeParserResult<TUser>> ParseAsync(Parameter parameter, string value, CommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            if (context.IsPrivate)
                return TypeParserResult<TUser>.Unsuccessful(context.Localize("requirecontext_guild"));

            TUser user = null;

            // Parse by ID or mention
            if (ulong.TryParse(value, out var id) || MentionUtils.TryParseUser(value, out id))
            {
                user = context.Guild.GetUser(id) as TUser;
            }

            // Parse by user#discrim
            if (user is null)
            {
                user = context.Guild.Users.FirstOrDefault(x =>
                    x.ToString().Equals(value, StringComparison.OrdinalIgnoreCase)) as TUser;
            }

            // Parse by exact username/nickname match
            if (user is null)
            {
                var matches = context.Guild.Users.Where(x =>
                        x.Username?.Equals(value, StringComparison.OrdinalIgnoreCase) == true ||
                        x.Nickname?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
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