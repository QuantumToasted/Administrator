using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Services;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Extensions
{
    public static class ServiceExtensions
    {
        public static ServiceCollection AutoAddServices(this ServiceCollection collection)
        {
            foreach (var type in Assembly.GetEntryAssembly().GetTypes()
                .Where(x => typeof(Service).IsAssignableFrom(x) && !x.IsAbstract))
            {
                collection.AddSingleton(type);
            }

            return collection;
        }

        public static async Task InitializeServicesAsync(this IServiceProvider provider)
        {
            foreach (var type in Assembly.GetEntryAssembly().GetTypes()
                .Where(x => typeof(Service).IsAssignableFrom(x) && !x.IsAbstract))
            {
                await ((Service) provider.GetRequiredService(type)).InitializeAsync();
            }
        }

        /*public static IEnumerable<IHandler<TArgs>> GetHandlers<TArgs>(this IServiceProvider provider)
            where TArgs : EventArgs
        {
            foreach (var type in Assembly.GetEntryAssembly().GetTypes().Where(x =>
                typeof(IHandler<TArgs>).IsAssignableFrom(x) && !x.IsInterface))
            {
                yield return (IHandler<TArgs>) provider.GetRequiredService(type);
            }
        }*/

        public static IEnumerable<IHandler> GetHandlers(this IServiceProvider provider, Type argType)
        {
            var handlerType = typeof(IHandler<>).MakeGenericType(argType);
            foreach (var type in Assembly.GetEntryAssembly().GetTypes().Where(x =>
                handlerType.IsAssignableFrom(x))
                .OrderByDescending(x => x.GetMethod("HandleAsync", new [] {argType})?
                                            .GetCustomAttribute<HandlerPriorityAttribute>()?.Value ?? 0))
            {
                yield return (IHandler) provider.GetRequiredService(type);
            }
        }
    }
}