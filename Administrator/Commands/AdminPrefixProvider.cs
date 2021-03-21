using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Commands
{
    public sealed class AdminPrefixProvider : IPrefixProvider
    {
        private readonly IPrefix _defaultPrefix;
        private readonly IServiceProvider _services;

        public AdminPrefixProvider(IConfiguration configuration, IServiceProvider services)
        {
            _defaultPrefix = new StringPrefix(configuration["DEFAULT_PREFIX"]);
            _services = services;
        }
        
        // TODO: fill out db stuff
        public async ValueTask<IEnumerable<IPrefix>> GetPrefixesAsync(IGatewayUserMessage message)
        {
            var prefixes = new List<IPrefix> {_defaultPrefix};

            if (!message.GuildId.HasValue)
                return prefixes;

            using var scope = _services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            if (await ctx.FindAsync<Guild>(message.GuildId.Value) is { } guild && 
                guild.Prefixes.Count > 0)
            {
                prefixes.AddRange(guild.Prefixes.Select(x => new StringPrefix(x)));
            }

            return prefixes;
        }
    }
}