using Administrator.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Utilities
{
    public static class ServiceUtilities
    {
        public static ServiceCollection AutoBuildServices()
        {
            var collection = new ServiceCollection();
            foreach (var type in Assembly.GetEntryAssembly().GetTypes()
                .Where(x => typeof(IService).IsAssignableFrom(x) && !x.IsInterface))
            {
                collection.AddSingleton(type);
            }

            return collection;
        }

        public static async Task InitializeServicesAsync(IServiceProvider provider)
        {
            foreach (var type in Assembly.GetEntryAssembly().GetTypes()
                .Where(x => typeof(IService).IsAssignableFrom(x) && !x.IsInterface))
            {
                await ((IService) provider.GetRequiredService(type)).InitializeAsync();
            }
        }
    }
}
