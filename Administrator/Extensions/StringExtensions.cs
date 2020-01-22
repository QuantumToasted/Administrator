using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Administrator.Commands;
using Disqord;

namespace Administrator.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex AsyncUserRegex =
            new Regex(@"{user\.(?:xp|level|nextxp|tier)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AsyncGuildUserRegex =
            new Regex(@"{user\.(?:guildxp|guildlevel|guildnextxp|guildtier)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RandomNumberRegex = 
            new Regex(@"{random(\d{1,})-(\d{1,})}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex LazyImageLinkRegex = new Regex(
            @"(http|https):\/\/.{2,}(png|jpg|jpeg|gif)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static StringBuilder AppendNewline(this StringBuilder builder, string text)
            => builder.Append(text).Append('\n');

        public static StringBuilder AppendNewline(this StringBuilder builder)
            => builder.Append('\n');

        public static bool HasImageExtension(this string str, out ImageFormat format)
        {
            format = ImageFormat.Default;
            if (string.IsNullOrWhiteSpace(str)) return false;

            switch (str.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.ToLower())
            {
                case "png":
                    format = ImageFormat.Png;
                    return true;
                case "jpeg":
                case "jpg":
                    format = ImageFormat.Jpg;
                    return true;
                case "gif":
                    format = ImageFormat.Gif;
                    return true;
                case "webp":
                    format = ImageFormat.WebP;
                    return true;
                default:
                    return false;
            }
        }

        public static string TrimTo(this string str, int length, bool useEllipses = false)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            if (!useEllipses)
                return str[..Math.Min(length, str.Length)];

            if (length > str.Length)
                return str;

            return str[..(length - 1)] + '…';
        }

        public static string FixateTo(this string str, ref int center, int truncateTo)
        {
            if (center > str.Length)
                throw new ArgumentOutOfRangeException(nameof(center));

            var trimStart = false;
            var trimEnd = false;
            while (str.Length > truncateTo)
            {
                if (center > str.Length / 2) // right of center
                {
                    trimStart = true;
                    str = str[1..str.Length];
                    center--;
                }
                else
                {
                    trimEnd = true;
                    str = str[..^1];
                }
            }

            if (trimStart)
            {
                str = '…' + str[1..str.Length];
            }

            if (trimEnd)
            {
                str = str[..^1] + '…';
            }

            return str;
        }

        public static int GetLevenshteinDistanceTo(this string str, string other)
        {
            str = str.ToLower();
            other = other.ToLower();

            var n = str.Length;
            var m = other.Length;
            var d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (var i = 0; i <= n; d[i, 0] = i++)
            { }

            for (var j = 0; j <= m; d[0, j] = j++)
            { }

            for (var i = 1; i <= n; i++)
            for (var j = 1; j <= m; j++)
            {
                var cost = other[j - 1] == str[i - 1] ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }

            return d[n, m];
        }

        public static string Sanitize(this string str)
            => Markdown.Escape(str); // TODO: options for what to sanitize.

        public static async Task<string> FormatPlaceHoldersAsync(this string str, AdminCommandContext context, object target = null, Random random = null)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            // Target
            if (!(target is null))
                str = str.Replace("{target}", target.ToString(), StringComparison.OrdinalIgnoreCase);

            // RNG
            str = RandomNumberRegex.Replace(str, ReplaceRandomNumber);

            // User
            if (AsyncUserRegex.IsMatch(str))
            {
                var user = await context.Database.GetOrCreateGlobalUserAsync(context.User.Id);
                str = str.Replace("{user.xp}", user.CurrentLevelXp.ToString(), StringComparison.OrdinalIgnoreCase)
                    .Replace("{user.level}", user.Level.ToString(), StringComparison.OrdinalIgnoreCase)
                    .Replace("{user.nextxp}", user.NextLevelXp.ToString(), StringComparison.OrdinalIgnoreCase)
                    .Replace("{user.tier}", user.Tier.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            if (AsyncGuildUserRegex.IsMatch(str) && !context.IsPrivate)
            {
                var guildUser = await context.Database.GetOrCreateGuildUserAsync(context.User.Id, context.Guild.Id);
                str = str.Replace("{user.guildxp}", guildUser.CurrentLevelXp.ToString(), StringComparison.OrdinalIgnoreCase)
                    .Replace("{user.guildlevel}", guildUser.Level.ToString(), StringComparison.OrdinalIgnoreCase)
                    .Replace("{user.guildnextxp}", guildUser.NextLevelXp.ToString(), StringComparison.OrdinalIgnoreCase)
                    .Replace("{user.guildtier}", guildUser.Tier.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            str = str.Replace("{user}", context.User.Tag, StringComparison.OrdinalIgnoreCase)
                .Replace("{user.id}", context.User.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{user.avatar}", context.User.GetAvatarUrl(), StringComparison.OrdinalIgnoreCase)
                .Replace("{user.name}", context.User.Name, StringComparison.OrdinalIgnoreCase)
                .Replace("{user.mention}", context.User.Mention, StringComparison.OrdinalIgnoreCase)
                .Replace("{user.created}", context.User.Id.CreatedAt.ToString("g", context.Language.Culture),
                    StringComparison.OrdinalIgnoreCase)
                .Replace("{user.discrim}", context.User.Discriminator, StringComparison.OrdinalIgnoreCase);

            if (!context.IsPrivate)
            {
                var member = (CachedMember) context.User;
                str = str.Replace("{user.nick}", member.Nick ?? context.User.Name, StringComparison.OrdinalIgnoreCase)
                    .Replace("{user.joined}", member.JoinedAt.ToString("g", context.Language.Culture),
                        StringComparison.OrdinalIgnoreCase);
            }

            // Channel
            str = str.Replace("{channel}", context.Channel.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{channel.id}", context.Channel.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{channel.name}", context.Channel.Name, StringComparison.OrdinalIgnoreCase)
                .Replace("{channel.created}", context.Channel.Id.CreatedAt.ToString("g", context.Language.Culture), StringComparison.OrdinalIgnoreCase);

            if (!context.IsPrivate)
            {
                var channel = (CachedTextChannel) context.Channel;
                str = str.Replace("{channel.tag}", channel.Tag, StringComparison.OrdinalIgnoreCase)
                    .Replace("{channel.topic}", channel.Topic, StringComparison.OrdinalIgnoreCase)
                    .Replace("{channel.mention}", channel.Mention, StringComparison.OrdinalIgnoreCase);
            }

            // Guild
            if (!context.IsPrivate)
            {
                str = str.Replace("{guild}", context.Guild.Name, StringComparison.OrdinalIgnoreCase)
                    .Replace("{guild.id}", context.Guild.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                    .Replace("{guild.name}", context.Guild.Name, StringComparison.OrdinalIgnoreCase)
                    .Replace("{guild.created}", context.Guild.Id.CreatedAt.ToString("g", context.Language.Culture), StringComparison.OrdinalIgnoreCase)
                    .Replace("{guild.members}", context.Guild.Members.Count.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            return str;

            string ReplaceRandomNumber(Match match)
            {
                try
                {
                    return (random ?? new Random())
                        .Next(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value))
                        .ToString();
                }
                catch
                {
                    return match.Value;
                }
            }
        }
    }
}