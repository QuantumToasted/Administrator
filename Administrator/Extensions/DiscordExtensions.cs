using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;

namespace Administrator.Extensions
{
    public static class DiscordExtensions
    {
        public static string Format(this IUser user, bool bold = true, bool code = true)
        {
            return new StringBuilder()
                .Append(bold ? Markdown.Bold(user.Tag.Sanitize()) : user.Tag.Sanitize())
                .Append(" (")
                .Append(code ? Markdown.Code(user.Id.ToString()) : user.Id.ToString())
                .Append(')')
                .ToString();
        }

        public static string Format(this IRole role, bool bold = true, bool code = true)
        {
            return new StringBuilder()
                .Append(bold ? Markdown.Bold(role.Name.Sanitize()) : role.Name.Sanitize())
                .Append(" (")
                .Append(code ? Markdown.Code(role.Id.ToString()) : role.Id.ToString())
                .Append(')')
                .ToString();
        }

        public static string Format(this IGuildChannel channel, bool bold = true, bool code = true)
        {
            return new StringBuilder()
                .Append(channel switch
                {
                    ITextChannel textChannel => textChannel.Mention,
                    _ => bold ? Markdown.Bold(channel.Name.Sanitize()) : channel.Name.Sanitize()
                })
                .Append(" (")
                .Append(code ? Markdown.Code(channel.Id.ToString()) : channel.Id.ToString())
                .Append(')')
                .ToString();
        }

        public static string Format(this IGuild guild, bool bold = true, bool code = true)
        {
            return new StringBuilder()
                .Append(bold ? Markdown.Bold(guild.Name.Sanitize()) : guild.Name.Sanitize())
                .Append(" (")
                .Append(code ? Markdown.Code(guild.Id.ToString()) : guild.Id.ToString())
                .Append(')')
                .ToString();
        }

        public static async ValueTask<IUser> GetOrFetchUserAsync(this DiscordClientBase client, Snowflake id)
        {
            if (client.GetUser(id) is { } cachedUser)
                return cachedUser;

            return await client.FetchUserAsync(id);
        }
        
        public static int GetEmojiLimit(this IGuild guild)
        {
            return guild.BoostTier switch
            {
                BoostTier.None => 50,
                BoostTier.First => 100,
                BoostTier.Second => 150,
                BoostTier.Third => 250,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static IRole GetHighestRole(this IMember member, Func<CachedRole, bool> predicate = null)
        {
            var roles = member.GetRoles().Values.OrderByDescending(x => x.Position);

            return predicate is not null
                ? roles.FirstOrDefault(predicate)
                : roles.FirstOrDefault();
        }
        
    }
}