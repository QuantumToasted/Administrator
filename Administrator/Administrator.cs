using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using FluentScheduler;
using Qmmands;

namespace Administrator
{
    public sealed class Administrator
    {
        public async Task InitializeAsync()
        {
            var config = ConfigurationService.Basic;
            var client = new DiscordClient(TokenType.Bot, config.DiscordToken,
                new DiscordClientConfiguration {MessageCache = new DefaultMessageCache(100)});

            var provider = new ServiceCollection()
                .AutoAddServices()
                .AddSingleton(client)
                .AddSingleton<CommandService>()
                .AddSingleton<CancellationTokenSource>()
                .AddSingleton<Random>()
                .AddSingleton<Registry>()
                .AddSingleton<HttpClient>()
                .AddEntityFrameworkNpgsql()
                .BuildServiceProvider();

            try
            {
                await provider.InitializeServicesAsync();
                await client.RunAsync(provider.GetRequiredService<CancellationTokenSource>().Token);
            }
            catch (TaskCanceledException)
            { }
            catch (Exception ex)
            {
                await provider.GetRequiredService<LoggingService>().LogCriticalAsync(ex, "Administrator");
            }
        }
    }
}