using Administrator.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Common;
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
        private int _recommendedShards;
        private int _shardsReady;
        private bool _ready;

        private async Task OnShardReadyAsync(DiscordSocketClient shard)
        {
            Log.Info($"Shard #[{_client.GetShardIdFor(shard.Guilds.First())}/{_client.Shards.Count}] ready.");

            if (_ready || ++_shardsReady != _recommendedShards) return;
            
            _ready = true;
            await ServiceUtilities.InitializeServicesAsync(_provider);
        }

        public async Task InitializeAsync()
        {
            var restClient = new DiscordRestClient();
            await restClient.LoginAsync(TokenType.Bot,
                new ConfigurationService(null).DiscordToken);
            _recommendedShards = await restClient.GetRecommendedShardCountAsync();
            
            _client = new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 100,
                LogLevel = LogSeverity.Info,
                TotalShards = _recommendedShards
            });
            
            _provider = ServiceUtilities.AutoBuildServices()
                .AddSingleton(_client)
                .AddSingleton(restClient)
                .AddSingleton<CommandService>()
                .AddSingleton<CancellationTokenSource>()
                .BuildServiceProvider();

            _client.ShardReady += OnShardReadyAsync;
            
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