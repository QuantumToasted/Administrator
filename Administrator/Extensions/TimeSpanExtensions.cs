using System;
using Administrator.Commands;
using Humanizer;
using Humanizer.Localisation;

namespace Administrator.Extensions
{
    public static class TimeSpanExtensions
    {
        public static string HumanizeFormatted(this TimeSpan ts, AdminCommandContext context, TimeUnit? minimum = null, bool ago = false)
        {
            var min = minimum ?? TimeUnit.Minute;
            var format = ts.Humanize(4, context.Language.Culture, minUnit: min, maxUnit: TimeUnit.Year);

            if (!ago)
                return format;

            var belowMinimum = false;
            switch (minimum)
            {
                case TimeUnit.Second:
                    belowMinimum = ts > TimeSpan.FromSeconds(1);
                    break;
                case TimeUnit.Minute:
                    belowMinimum = ts > TimeSpan.FromMinutes(1);
                    break;
                case TimeUnit.Hour:
                    belowMinimum = ts > TimeSpan.FromHours(1);
                    break;
                case TimeUnit.Day:
                    belowMinimum = ts > TimeSpan.FromDays(1);
                    break;
            }

            return belowMinimum
                ? context.Localize("info_now")
                : context.Localize("info_ago", format);
        }
        /*
        public static string HumanizeFormatted(this TimeSpan ts, AdminCommandContext context, bool ago,)
        {
            if (ts < TimeSpan.FromMinutes(1))
            return ts < TimeSpan.FromMinutes(1)
                ? context.Localize("info_now")
                : context.Localize("info_ago", );
        }
        */
    }
}