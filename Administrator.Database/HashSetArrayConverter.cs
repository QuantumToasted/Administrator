using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.ValueConversion;

namespace Administrator.Database;

public class HashSetArrayConverter<T>() : ValueConverter<HashSet<T>, T[]>(static x => x.ToArray(), static x => x.ToHashSet()), INpgsqlArrayConverter
    where T : notnull
{
    public ValueConverter ElementConverter => new ValueConverter<T, T>(static x => x, static x => x);
}