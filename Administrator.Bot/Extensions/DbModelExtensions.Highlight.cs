using System.Text.RegularExpressions;
using Administrator.Database;
using Disqord;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static bool IsMatch(this Highlight highlight, IMessage message)
    {
        return new Regex($@"(^|\b){Regex.Escape(highlight.Text)}($|\b)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
            .IsMatch(message.Content);
    }
}