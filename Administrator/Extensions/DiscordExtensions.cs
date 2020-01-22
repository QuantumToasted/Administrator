using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;

namespace Administrator.Extensions
{
    public static class DiscordExtensions
    {
        public static string Format(this IUser user, bool bold = true, bool code = true)
            => user is null
                ? null
                : $"{(bold ? Markdown.Bold(user.Tag.Sanitize()) : user.Tag.Sanitize())} ({(code ? Markdown.Code(user.Id.ToString()) : user.Id.ToString())})";

        public static string Format(this IRole role, bool bold = true, bool code = true)
            => role is null
                ? null
                : $"{(bold ? Markdown.Bold(role.Name.Sanitize()) : role.Name.Sanitize())} ({(code ? Markdown.Code(role.Id.ToString()) : role.Id.ToString())})";

        public static string Format(this IGuildChannel channel, bool bold = true, bool code = true)
            => channel switch
            {
                ITextChannel textChannel => $"{textChannel.Mention} ({(code ? Markdown.Code(channel.Id.ToString()) : channel.Id.ToString())})",
                IVoiceChannel voiceChannel => $"{(bold ? Markdown.Bold(voiceChannel.Name.Sanitize()) : voiceChannel.Name.Sanitize())} ({(code ? Markdown.Code(channel.Id.ToString()) : channel.Id.ToString())})",
                ICategoryChannel category => $"{(bold ? Markdown.Bold(category.Name.Sanitize()) : category.Name.Sanitize())} ({(code ? Markdown.Code(channel.Id.ToString()) : channel.Id.ToString())})",
                _ => null
            };

        public static string Format(this IGuild guild, bool bold = true, bool code = true)
            => guild is null
                ? null
                : $"{(bold ? Markdown.Bold(guild.Name.Sanitize()) : guild.Name.Sanitize())} ({(code ? Markdown.Code(guild.Id.ToString()) : guild.Id.ToString())})";

        public static CachedRole GetHighestRole(this CachedMember user, Func<CachedRole, bool> func)
            => user.Roles.Values.OrderByDescending(x => x.Position).Where(func).FirstOrDefault();

        public static CachedRole GetHighestRole(this CachedMember user)
            => user.Roles.Values.OrderByDescending(x => x.Position).First();

        public static async ValueTask<IUser> GetOrDownloadUserAsync(this DiscordClientBase client, ulong id)
            => client.GetUser(id) ?? await client.GetUserAsync(id) as IUser;

        public static int GetEmojiLimit(this IGuild guild)
        {
            return guild.BoostTier switch
            {
                BoostTier.None => 50,
                BoostTier.First => 100,
                BoostTier.Second => 150,
                BoostTier.Third => 250,
                _ => throw new ArgumentOutOfRangeException(nameof(guild.BoostTier))
            };
        }
    }
}