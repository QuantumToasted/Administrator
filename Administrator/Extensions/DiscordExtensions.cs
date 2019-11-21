using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;

namespace Administrator.Extensions
{
    public static class DiscordExtensions
    {
        public static string Format(this IUser user, bool bold = true)
            => user is null
                ? null
                : $"{(bold ? Markdown.Bold(user.ToString().Sanitize()) : user.ToString().Sanitize())} (`{user.Id}`)";

        public static string Format(this IRole role, bool bold = true)
            => role is null
                ? null
                : $"{(bold ? Markdown.Bold(role.Name.Sanitize()) : role.Name.Sanitize())} (`{role.Id}`)";

        public static string Format(this IGuildChannel channel, bool bold = true)
            => channel switch
            {
                ITextChannel textChannel => $"{textChannel.Mention} (`{channel.Id}`)",
                IVoiceChannel voiceChannel => $"{(bold ? Markdown.Bold(voiceChannel.Name.Sanitize()) : voiceChannel.Name.Sanitize())} (`{voiceChannel.Id}`)",
                ICategoryChannel category => $"{(bold ? Markdown.Bold(category.Name.Sanitize()) : category.Name.Sanitize())} (`{category.Id}`)",
                _ => null
            };

        public static string Format(this IGuild guild, bool bold = true)
            => guild is null
                ? null
                : $"{(bold ? Markdown.Bold(guild.Name.Sanitize()) : guild.Name.Sanitize())} (`{guild.Id}`)";

        public static CachedRole GetHighestRole(this CachedMember user, Func<CachedRole, bool> func)
            => user.Roles.Values.OrderByDescending(x => x.Position).Where(func).FirstOrDefault();

        public static CachedRole GetHighestRole(this CachedMember user)
            => user.Roles.Values.OrderByDescending(x => x.Position).First();

        public static async ValueTask<IUser> GetOrDownloadUserAsync(this DiscordClient client, ulong id)
            => client.GetUser(id) ?? await client.GetUserAsync(id) as IUser;
    }
}