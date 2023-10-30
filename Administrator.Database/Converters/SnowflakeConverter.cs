using Disqord;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Administrator.Database;

public class SnowflakeConverter() : ValueConverter<Snowflake, long>(x => (long) x.RawValue, x => new Snowflake((ulong) x));