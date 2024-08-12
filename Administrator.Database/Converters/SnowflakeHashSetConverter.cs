/*
using Disqord;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Administrator.Database;

public sealed class SnowflakeHashSetConverter() : NpgsqlHashSetConverter<Snowflake, long>(x => (long) x.RawValue, x => new Snowflake((ulong) x));

public sealed class SnowflakeHashSetConverter() : ValueConverter<HashSet<Snowflake>, long[]>(
    x => x.Select(y => (long) y.RawValue).ToArray(),
    x => x.Select(y => new Snowflake((ulong) y)).ToHashSet());
    */