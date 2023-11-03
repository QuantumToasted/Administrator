using Disqord;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Administrator.Database;

public sealed class NullableSnowflakeConverter() : ValueConverter<Snowflake?, long?>(
    x => x != null ? (long) x.Value.RawValue : null, 
    x => x != null ? new Snowflake((ulong) x) : null);