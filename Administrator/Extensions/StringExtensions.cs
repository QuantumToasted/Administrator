using System;
using System.Text.RegularExpressions;

namespace Administrator.Extensions
{
    public static class StringExtensions
    {
        public static readonly Regex LazyImageLinkRegex = new Regex(
            @"(http|https):\/\/.{2,}(png|jpg|jpeg|gif)");

        public static bool HasImageExtension(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return false;
            return str.EndsWith(".png") || str.EndsWith(".jpg") || str.EndsWith(".jpeg") || str.EndsWith(".gif") ||
                   str.EndsWith(".bmp");
        }

        public static string TrimTo(this string str, int length, bool useEllipses = false)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            if (!useEllipses)
                return str[..Math.Min(length, str.Length)];

            if (length > str.Length)
                return str;

            return str[..length - 1] + '…';
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
    }
}