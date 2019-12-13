using System;
using System.Globalization;

namespace Administrator.Common
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class BuildDateAttribute : Attribute
    {
        public BuildDateAttribute(string value)
        {
            Date = DateTimeOffset.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal);
        }

        public DateTimeOffset Date { get; }
    }
}