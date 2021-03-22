using System;
using Humanizer;
using Humanizer.Localisation;

namespace Administrator.Extensions
{
    public static class TimeExtensions
    {
        public static string HumanizeFormatted(this TimeSpan ts, bool ago = false)
        {
            if (ts < TimeSpan.FromSeconds(1))
                return $"{ts.TotalSeconds:F} seconds";

            return ago
                ? $"{ts.Humanize(int.MaxValue, minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year)} ago"
                : ts.Humanize(int.MaxValue, minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year);
        }

        public static string FormatAsCreated(this DateTimeOffset dto)
            => string.Join('\n', $"{dto:g} UTC", (DateTimeOffset.UtcNow - dto).HumanizeFormatted(true));
    }
}