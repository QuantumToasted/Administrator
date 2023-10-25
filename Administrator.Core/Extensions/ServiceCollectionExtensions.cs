using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScopedServices(this IServiceCollection services)
    {
        foreach (var type in Assembly.GetEntryAssembly()!.GetTypes().Where(x => x.GetCustomAttribute<ScopedServiceAttribute>() is not null))
        {
            services.AddScoped(type);
        }

        return services;
    }
}