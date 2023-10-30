using System.Text.RegularExpressions;
using Administrator.Database;
using Disqord;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
    
    public static bool IsMatch(this ForumAutoTag autoTag, IMessage message)
    {
        if (autoTag.IsRegex)
        {
            try
            {
                var match = new Regex(autoTag.Text, RegexOptions.None, RegexTimeout).Match(message.Content);
                return match.Success;
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        return message.Content.Contains(autoTag.Text, StringComparison.InvariantCultureIgnoreCase);
    }
}