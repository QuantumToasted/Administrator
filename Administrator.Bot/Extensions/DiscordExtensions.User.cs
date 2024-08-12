using Disqord;

namespace Administrator.Bot;

public static partial class DiscordExtensions
{
    public static string Format(this IUser user)
        => $"{Markdown.Bold(user.Name)} ({Markdown.Code(user.Id)})";
}