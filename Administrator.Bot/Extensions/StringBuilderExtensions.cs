using System.Text;

namespace Administrator.Bot;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendNewline(this StringBuilder sb, string? text = null)
        => sb.Append($"{text}\n");

    public static StringBuilder AppendJoinTruncated<T>(this StringBuilder sb, string separator, IEnumerable<T> values, int length, Func<int, string>? remainderFormatter = null)
    {
        remainderFormatter ??= static i => $"{i} more...";

        var list = values.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var formatted = list[i]!.ToString();

            // subtract an extra position:
            // if length is too long we need to ignore the current value of i
            var remainderLine = remainderFormatter(list.Count - i - 1);

            if (sb.Length + formatted!.Length + separator.Length + remainderLine.Length >= length)
            {
                return sb.Append(separator).Append(remainderLine);
            }

            if (i > 0)
                sb.Append(separator);

            sb.Append(formatted);
        }

        return sb;
    }
}