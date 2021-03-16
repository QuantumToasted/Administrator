using System;
using Serilog.Core;
using Serilog.Events;

namespace Administrator.Common
{
    public sealed class LogEventFilter : ILogEventFilter
    {
        private static readonly string[] IgnoreTextEqualing =
        {

        };

        private static readonly string[] IgnoreTextStartingWith =
        {
            "An exception occurred while executing "
        };
        
        // IsEnabled = "should we log this?"
        public bool IsEnabled(LogEvent logEvent)
        {
            var text = logEvent.MessageTemplate.Text;
            
            foreach (var str in IgnoreTextEqualing)
            {
                if (text.Equals(str, StringComparison.InvariantCultureIgnoreCase))
                    return false;
            }

            foreach (var str in IgnoreTextStartingWith)
            {
                if (text.StartsWith(str, StringComparison.InvariantCultureIgnoreCase))
                    return false;
            }

            return true;
        }
    }
}