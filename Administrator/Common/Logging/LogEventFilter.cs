using Serilog.Core;
using Serilog.Events;

namespace Administrator.Common
{
    public sealed class LogEventFilter : ILogEventFilter
    {
        // IsEnabled = "should we log this?"
        public bool IsEnabled(LogEvent logEvent)
        {
            return !logEvent.MessageTemplate.Text.Equals("Unknown message and author has no value in MessageUpdated.");
        }
    }
}