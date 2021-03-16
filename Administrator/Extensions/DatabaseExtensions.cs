using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Administrator.Extensions
{
    public static class DatabaseExtensions
    {
        public static ModelBuilder DefineGlobalConversion<TModel, TProvider>(this ModelBuilder builder,
            Expression<Func<TModel, TProvider>> outExpr,
            Expression<Func<TProvider, TModel>> inExpr)
        {
            foreach (var type in builder.Model.GetEntityTypes())
            foreach (var property in type.ClrType.GetProperties().Where(x => x.PropertyType == typeof(TModel)))
            {
                builder.Entity(type.Name).Property(property.Name).HasConversion(new ValueConverter<TModel, TProvider>(outExpr, inExpr));
            }

            return builder;
        }
    }
}