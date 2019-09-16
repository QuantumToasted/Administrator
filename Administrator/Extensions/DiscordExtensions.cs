using System;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace Administrator.Extensions
{
    public static class DiscordExtensions
    {
        public static string Format(this IUser user, bool bold = true)
            => user is null ? null : $"{(bold ? Discord.Format.Bold(user.ToString()) : user.ToString())} (`{user.Id}`)";

        public static string Format(this IRole role, bool bold = true)
            => role is null ? null : $"{(bold ? Discord.Format.Bold(role.Name) : role.Name)} (`{role.Id}`)";

        public static string Format(this ITextChannel channel)
            => channel is null ? null : $"{channel.Mention} (`{channel.Id}`)";

        public static string Format(this IVoiceChannel channel, bool bold = true)
            => channel is null ? null : $"{(bold ? Discord.Format.Bold(channel.Name) : channel.Name)} (`{channel.Id}`)";

        public static string Format(this IGuild guild, bool bold = true)
            => guild is null ? null : $"{(bold ? Discord.Format.Bold(guild.Name) : guild.Name)} (`{guild.Id}`)";

        public static SocketRole GetHighestRole(this SocketGuildUser user, Func<SocketRole, bool> func)
            => user.Roles.OrderByDescending(x => x.Position).Where(func).First();

        public static SocketRole GetHighestRole(this SocketGuildUser user)
            => user.Roles.OrderByDescending(x => x.Position).First();
    }
}