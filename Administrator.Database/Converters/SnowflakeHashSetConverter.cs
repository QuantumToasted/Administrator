using Disqord;

namespace Administrator.Database;

public class SnowflakeHashSetConverter() : NpgsqlHashSetConverter<Snowflake, long>(
    hashSet => hashSet.Select(x => (long) x.RawValue).ToList(),
    list => list.Select(x => new Snowflake((ulong) x)).ToHashSet(),
    x => (long) x.RawValue,
    x => new Snowflake((ulong) x));