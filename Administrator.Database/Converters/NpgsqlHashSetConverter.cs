/*
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.ValueConversion;

namespace Administrator.Database;

public class NpgsqlHashSetConverter<TModel, TProvider>(Expression<Func<TModel, TProvider>> outExpr, Expression<Func<TProvider, TModel>> inExpr)
    : NpgsqlArrayConverter<HashSet<TModel>, List<TProvider>, TProvider[]>(new ValueConverter<TModel, TProvider>(outExpr, inExpr));
    */