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
        private IServiceProvider _provider;
        private DiscordShardedClient _client;

        public async Task InitializeAsync()
        {
            var restClient = new DiscordRestClient();
            var config = ConfigurationService.Basic;
            
            await restClient.LoginAsync(TokenType.Bot, config.DiscordToken);
            
            _client = new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 100,
                LogLevel = LogSeverity.Info,
                TotalShards = await restClient.GetRecommendedShardCountAsync()
            });
            
            _provider = ServiceUtilities.AutoBuildServices()
                .AddSingleton(_client)
                .AddSingleton(restClient)
                .AddSingleton<CommandService>()
                .AddSingleton<CancellationTokenSource>()
                .BuildServiceProvider();
            
            await ServiceUtilities.InitializeServicesAsync(_provider);
            await _client.LoginAsync(TokenType.Bot, _provider.GetRequiredService<ConfigurationService>().DiscordToken);
            await _client.StartAsync();

            try
            {
                await Task.Delay(-1, _provider.GetRequiredService<CancellationTokenSource>().Token);
            }
            catch (TaskCanceledException)
            { }
        }
    }
}