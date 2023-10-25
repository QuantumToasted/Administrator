using NodaTime;
using NodaTime.Extensions;

namespace Administrator.Core;

public static class DateTimeExtensions
{
    public static readonly IReadOnlyDictionary<string, TimeZoneInfo> IanaTimeZoneMap;
    
    static DateTimeExtensions()
    {
        var dict = new Dictionary<string, TimeZoneInfo>();
        
        foreach (var tz in DateTimeZoneProviders.Tzdb.GetAllZones())
        {
            try
            {
                dict[tz.Id] = TimeZoneInfo.FindSystemTimeZoneById(tz.Id);
            }
            catch (TimeZoneNotFoundException)
            { }
        }

        IanaTimeZoneMap = dict;
    }

    public static DateTimeOffset AddWeeks(this DateTimeOffset dto, int weeks)
        => dto.AddDays(weeks * 7);
}