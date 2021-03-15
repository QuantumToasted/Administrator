using System;
using Serilog.Core;
using Serilog.Events;

namespace Administrator.Common
{
    public sealed class LogEventEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var fullName = logEvent.Properties["SourceContext"].ToString();

            if (fullName.Contains('.'))
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SourceContext",
                    fullName.Split('.', StringSplitOptions.RemoveEmptyEntries)[^1].TrimEnd('"')));
            }
        }
    }
}