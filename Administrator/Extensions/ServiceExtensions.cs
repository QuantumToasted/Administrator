using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Administrator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Extensions
{
    public static class ServiceExtensions
    {
        public static ServiceCollection AutoAddServices(this ServiceCollection collection)
        {
            foreach (var type in Assembly.GetEntryAssembly().GetTypes()
                .Where(x => typeof(IService).IsAssignableFrom(x) && !x.IsInterface))
            {
                collection.AddSingleton(type);
            }

            return collection;
        }

        public static async Task InitializeServicesAsync(this IServiceProvider provider)
        {
            foreach (var type in Assembly.GetEntryAssembly().GetTypes()
                .Where(x => typeof(IService).IsAssignableFrom(x) && !x.IsInterface))
            {
                await ((IService) provider.GetRequiredService(type)).InitializeAsync();
            }
        }
    }
}