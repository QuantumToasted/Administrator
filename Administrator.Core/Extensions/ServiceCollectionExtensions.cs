using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Administrator.Core;

public static class ServiceCollectionExtensions
{
    extension(ServiceCollection services)
    {
        public ServiceCollection AddAdminService<TService>() where TService : class, IAdminService<TService>
        {
            services.TryAddSingleton<TService>();

            services.AddSingleton<IAdminService<TService>>(x => x.GetRequiredService<TService>());
            services.AddKeyedSingleton<IAdminService>(typeof(TService));

            return services;
        }
    }
}