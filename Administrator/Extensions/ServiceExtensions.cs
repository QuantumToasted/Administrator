using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Administrator.Services;
using Disqord.Events;
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

        public static IEnumerable<IHandler<TArgs>> GetHandlers<TArgs>(this IServiceProvider provider)
            where TArgs : EventArgs
        {
            foreach (var type in Assembly.GetEntryAssembly().GetTypes().Where(x =>
                typeof(IHandler<TArgs>).IsAssignableFrom(x) && !x.IsInterface))
            {
                yield return (IHandler<TArgs>) provider.GetRequiredService(type);
            }
        }

        public static IEnumerable<IHandler> GetHandlers(this IServiceProvider provider, Type argType)
        {
            var handlerType = typeof(IHandler<>).MakeGenericType(argType);
            foreach (var type in Assembly.GetEntryAssembly().GetTypes().Where(x =>
                handlerType.IsAssignableFrom(x)))
            {
                yield return (IHandler) provider.GetRequiredService(type);
            }
        }
    }
}