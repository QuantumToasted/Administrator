using System.Text;

namespace Administrator.Bot;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendNewline(this StringBuilder sb, string? text = null)
        => sb.Append($"{text}\n");

    public static StringBuilder AppendJoinTruncated<T>(this StringBuilder sb, string separator, IEnumerable<T> values, int length, Func<int, string>? remainderFormatter = null)
        where T : notnull
    {
        remainderFormatter ??= static i => $"{i} more...";

        var list = values.ToList();
        string? nextFormatted = null;
        for (var i = 0; i < list.Count; i++)
        {
            var formatted = nextFormatted ?? list[i].ToString();
            
            // let's assume that the first, single element will not exceed 'length'...
            sb.Append(formatted);

            if (i > 0 && i < list.Count - 1)
                sb.Append(separator);

            if (i == list.Count - 1)
                continue;
            
            nextFormatted = list[i + 1].ToString();
            var remainderLine = remainderFormatter(list.Count - i - 1);

            if (sb.Length + nextFormatted!.Length + separator.Length + remainderLine.Length >= length)
            {
                return sb.Append(remainderLine);
            }

            // subtract an extra position:
            // if length is too long we need to ignore the current value of i
            //var remainderLine = remainderFormatter(list.Count - i - 1);

            /*
            if (sb.Length + formatted!.Length + separator.Length + remainderLine.Length >= length)
            {
                return sb.Append(separator).Append(remainderLine);
            }

            if (i > 0)
                sb.Append(separator);

            sb.Append(formatted);
            */
        }

        return sb;
    }
}