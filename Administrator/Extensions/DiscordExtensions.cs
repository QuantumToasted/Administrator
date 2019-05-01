using Discord;

namespace Administrator.Extensions
{
    public static class DiscordExtensions
    {
        public static string Format(this IUser user, bool bold = true)
            => user is null ? null : $"{(bold ? Discord.Format.Bold(user.ToString()) : user.ToString())} (`{user.Id}`)";
    }
}