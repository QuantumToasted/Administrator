using System;
using System.Linq;
using System.Text.RegularExpressions;
using Disqord;

namespace Administrator.Extensions
{
    public static class StringExtensions
    {
        public static readonly Regex LazyImageLinkRegex = new Regex(
            @"(http|https):\/\/.{2,}(png|jpg|jpeg|gif)", RegexOptions.Compiled);

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
    }
}