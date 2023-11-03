using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public static class EntityTypeBuilderExtensions
{
    public static PropertyBuilder<TProperty> HasPropertyWithColumnName<TEntity, TProperty>(this EntityTypeBuilder<TEntity> builder, 
        Expression<Func<TEntity, TProperty>> propertyExpression, string name)
        where TEntity : class
    {
        return builder.Property(propertyExpression).HasColumnName(name);
    }
}