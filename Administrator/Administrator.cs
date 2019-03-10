using Administrator.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
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
            var restClient = new DiscordRestClient();
            var config = ConfigurationService.Basic;
            
            await restClient.LoginAsync(TokenType.Bot, config.DiscordToken);
            
            var client = new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 100,
                LogLevel = LogSeverity.Info,
                TotalShards = await restClient.GetRecommendedShardCountAsync()
            });
            
            var provider = ServiceUtilities.AutoBuildServices()
                .AddSingleton(client)
                .AddSingleton(restClient)
                .AddSingleton<CommandService>()
                .AddSingleton<CancellationTokenSource>()
                .AddSingleton<Random>()
                .BuildServiceProvider();
            
            await ServiceUtilities.InitializeServicesAsync(provider);
            await client.LoginAsync(TokenType.Bot, provider.GetRequiredService<ConfigurationService>().DiscordToken);
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