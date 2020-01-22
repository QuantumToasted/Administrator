using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Extensions;
using Administrator.Services;
using Backpack.Net;
using Disqord;
using FluentScheduler;
using Qmmands;
using SteamWebAPI2.Utilities;

namespace Administrator
{
    public sealed class Administrator
    {
        public async Task InitializeAsync()
        {
            var config = ConfigurationService.Basic;
            var client = new DiscordClient(TokenType.Bot, config.DiscordToken,
                new DiscordClientConfiguration {MessageCache = new DefaultMessageCache(100)});
            var factory = new SteamWebInterfaceFactory(config.SteamApiKey); // TODO: Factory?
            var backpackClient = new BackpackClient(config.BackpackApiKey);
            var commands = new CommandService(new CommandServiceConfiguration
                {CooldownProvider = new AdminCooldownProvider()});

            var provider = new ServiceCollection()
                .AutoAddServices()
                .AddSingleton(client)
                .AddSingleton(factory)
                .AddSingleton(backpackClient)
                .AddSingleton(commands)
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