using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyStaticConfigurationsFromAssembly(this ModelBuilder modelBuilder, Assembly assembly,
        Func<Type, bool>? predicate = null)
    {
        var applyStaticEntityConfigurationMethod = typeof(ModelBuilderExtensions)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Single(
                e => e.Name == nameof(ApplyStaticConfiguration)
                     && e.GetGenericArguments().FirstOrDefault()?.GetGenericParameterConstraints().FirstOrDefault()?.GetGenericTypeDefinition()
                     == typeof(IStaticEntityTypeConfiguration<>));
        
        foreach (var type in assembly.GetTypes().OrderBy(x => x.FullName))
        {
            if (!predicate?.Invoke(type) ?? false)
                continue;

            foreach (var @interface in type.GetInterfaces())
            {
                if (!@interface.IsGenericType)
                {
                    continue;
                }

                if (@interface.GetGenericTypeDefinition() == typeof(IStaticEntityTypeConfiguration<>))
                {
                    var target = applyStaticEntityConfigurationMethod.MakeGenericMethod(type, @interface.GenericTypeArguments[0]);
                    target.Invoke(null, new object[] { modelBuilder });
                }
            }
        }

        return modelBuilder;
    }
    
    public static ModelBuilder ApplyStaticConfiguration<TConfiguration, TEntity>(this ModelBuilder builder)
        where TConfiguration : IStaticEntityTypeConfiguration<TEntity>
        where TEntity : class
    {
        TConfiguration.ConfigureBuilder(builder.Entity<TEntity>());
        return builder;
    }
}