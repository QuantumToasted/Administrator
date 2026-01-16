using System.Text;

namespace Administrator.Core;

public static class StringBuilderExtensions
{
    public delegate string RemainderFormatter(int remainingItems);
    
    extension(StringBuilder builder)
    {
        public StringBuilder AppendConditional(bool condition, object value)
        {
            if (condition)
                builder.Append(value);

            return builder;
        }

        public StringBuilder AppendConditional<T>(T state, Func<T, bool> condition, Func<T, object> value)
        {
            if (condition.Invoke(state))
                builder.Append(value.Invoke(state));

            return builder;
        }

        public StringBuilder AppendNewline(object value)
        {
            return builder.Append(value).Append('\n');
        }

        public StringBuilder AppendJoinTruncated<T>(ReadOnlySpan<char> separator, IEnumerable<T> values, int maxLength, RemainderFormatter? remainderFormatter = null)
            where T : notnull
        {
            remainderFormatter ??= static remaining => $"{remaining} more…";
            
            var list = values as IList<T> ?? values.ToList();
            string? nextFormatted = null;
            for (var i = 0; i < list.Count; i++)
            {
                var currentFormatted = nextFormatted ?? list[i].ToString();
                
                // let's assume that the first, single element will not exceed 'maxLength'...
                builder.Append(currentFormatted);
                
                // append the separator only if we're not at the end
                if (i != list.Count - 1)
                    builder.Append(separator);
                
                // break if we're at the end of the list
                if (i == list.Count - 1)
                    break;

                nextFormatted = list[i + 1].ToString();
                var remainderFormatted = remainderFormatter.Invoke(list.Count - i - 1);

                if (builder.Length + nextFormatted!.Length + separator.Length + remainderFormatted.Length >= maxLength)
                {
                    builder.Append(remainderFormatted);
                    break;
                }
            }

            return builder;
        }
    }
}