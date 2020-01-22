using System;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Services
{
    public sealed class CyclingStatusService : Service
    {
        private readonly DiscordClient _client;
        private readonly Random _random;

        public CyclingStatusService(IServiceProvider provider)
            : base(provider)
        {
            _client = _provider.GetRequiredService<DiscordClient>();
            _random = _provider.GetRequiredService<Random>();

            _provider.GetRequiredService<Registry>().Schedule(async () => await CycleStatusAsync())
                .NonReentrant()
                .ToRunNow()
                .AndEvery(2)
                .Minutes();
        }

        private async Task CycleStatusAsync()
        {
            using var ctx = new AdminDatabaseContext(_provider);
            var statuses = await ctx.Statuses.ToListAsync();
            if (statuses.Count == 0)
                return;

            var status = statuses.GetRandomElement(_random);
            await _client.SetPresenceAsync(new LocalActivity(status.Text, status.Type));
        }
    }
}