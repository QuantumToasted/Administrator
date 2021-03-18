using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Disqord.Gateway;
using Disqord.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Administrator.Services
{
    public sealed class DatabaseCleanupService : DiscordClientService
    {
        private readonly IServiceProvider _services;
        
        public DatabaseCleanupService(ILogger<DatabaseCleanupService> logger, AdministratorBot bot)
            : base(logger, bot)
        {
            _services = bot.Services;
            Client.LeftGuild += HandleLeftGuildAsync;
        }

        private async Task HandleLeftGuildAsync(object sender, LeftGuildEventArgs e)
        {
            using var scope = _services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            if (await ctx.FindAsync<Guild>(e.GuildId) is { } guild)
            {
                ctx.Guilds.Remove(guild);
            }

            await ctx.SaveChangesAsync();
        }
    }
}