using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.ValueConversion;

namespace Administrator.Database;

public class NpgsqlHashSetConverter<TModel, TProvider>(Expression<Func<HashSet<TModel>, List<TProvider>>> convertToProviderExpression,
        Expression<Func<List<TProvider>, HashSet<TModel>>> convertFromProviderExpression,
        Expression<Func<TModel, TProvider>> outExpr,
        Expression<Func<TProvider, TModel>> inExpr)
    : ValueConverter<HashSet<TModel>, List<TProvider>>(convertToProviderExpression, convertFromProviderExpression), INpgsqlArrayConverter
{
    public ValueConverter ElementConverter => new ValueConverter<TModel, TProvider>(outExpr, inExpr);
}