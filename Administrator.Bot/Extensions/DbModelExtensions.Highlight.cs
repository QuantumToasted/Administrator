using System.Text.RegularExpressions;
using Administrator.Database;
using Disqord;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static bool IsMatch(this Highlight highlight, IMessage message)
    {
        var escaped = Regex.Escape(highlight.Text);
        return new Regex($@"(^|\W+)({escaped})($|\W+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
            .IsMatch(message.Content);
    }
}