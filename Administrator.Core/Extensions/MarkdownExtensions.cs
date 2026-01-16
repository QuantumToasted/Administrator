using Disqord;
using NodaTime;

namespace Administrator.Core;

public static class MarkdownExtensions
{
    extension(Markdown)
    {
        public static string Timestamp(Instant instant, Markdown.TimestampFormat format = Markdown.TimestampFormat.RelativeTime)
            => Markdown.Timestamp(instant.ToDateTimeOffset(), format);
    }
}