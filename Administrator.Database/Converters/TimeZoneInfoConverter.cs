using System.Linq.Expressions;
using Administrator.Core;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Administrator.Database;

public class TimeZoneInfoConverter() : ValueConverter<TimeZoneInfo?, string?>(ConvertToProviderExpression, ConvertFromProviderExpression)
{
    private new static readonly Expression<Func<TimeZoneInfo?, string?>> ConvertToProviderExpression = 
        static x => x != null ? x.Id : null;

    private new static readonly Expression<Func<string?, TimeZoneInfo?>> ConvertFromProviderExpression =
        static x => x != null ?  DateTimeExtensions.IanaTimeZoneMap[x] : null;
}