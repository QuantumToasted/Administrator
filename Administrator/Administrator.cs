using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Extensions;
using Administrator.Services;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Qmmands;

namespace Administrator
{
    public sealed class Administrator
    {
        public async Task InitializeAsync()
        {
            var config = ConfigurationService.Basic;
            var restClient = new DiscordRestClient();
            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 100,
                LogLevel = LogSeverity.Info
            });

            var provider = new ServiceCollection()
                .AutoAddServices()
                .AddSingleton(client)
                .AddSingleton(restClient)
                .AddSingleton<CommandService>()
                .AddSingleton<CancellationTokenSource>()
                .AddSingleton<Random>()
                .AddEntityFrameworkNpgsql()
                .BuildServiceProvider();

            await restClient.LoginAsync(TokenType.Bot,config.DiscordToken);
            await provider.InitializeServicesAsync();
            await client.LoginAsync(TokenType.Bot, config.DiscordToken);
            await client.StartAsync();

            try
            {
                await Task.Delay(-1, provider.GetRequiredService<CancellationTokenSource>().Token);
            }
            catch (TaskCanceledException)
            { }
        }
    }
}