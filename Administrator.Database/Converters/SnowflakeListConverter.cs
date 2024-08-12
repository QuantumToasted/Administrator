using Disqord;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.ValueConversion;

namespace Administrator.Database;

public class SnowflakeListConverter()
    : NpgsqlArrayConverter<List<Snowflake>, List<Snowflake>, long[]>(
        new ValueConverter<Snowflake, long>(static x => (long)x.RawValue, static x => new Snowflake((ulong)x)));