using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Administrator.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScopedServices(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(x => x.GetCustomAttribute<ScopedServiceAttribute>() is not null))
        {
            services.AddScoped(type);
        }

        return services;
    }

    public static IServiceCollection AddConfiguration<TConfiguration>(this IServiceCollection services, IConfiguration hostConfiguration, out TConfiguration configuration)
        where TConfiguration : class, IAdministratorConfiguration<TConfiguration>, new()
    {
        configuration = new TConfiguration();
        hostConfiguration.GetSection(IAdministratorConfiguration<TConfiguration>.SectionName)
            .Bind(configuration);

        return services.AddConfiguration<TConfiguration>();
    }
    
    public static IServiceCollection AddConfiguration<TConfiguration>(this IServiceCollection services)
        where TConfiguration : class, IAdministratorConfiguration<TConfiguration>
    {
        services.AddOptions<TConfiguration>()
            .BindConfiguration(IAdministratorConfiguration<TConfiguration>.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}