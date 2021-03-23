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

        public AdminPrefixProvider(IConfiguration configuration)
        {
            _defaultPrefix = new StringPrefix(configuration["DEFAULT_PREFIX"]);
        }
        
        public async ValueTask<IEnumerable<IPrefix>> GetPrefixesAsync(IGatewayUserMessage message)
        {
            var prefixes = new List<IPrefix> {_defaultPrefix};
            var bot = (AdministratorBot) message.Client;

            if (!message.GuildId.HasValue || bot.GetGuild(message.GuildId.Value) is not { } g)
                return prefixes;

            using var scope = bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            
            var guild = await ctx.GetOrCreateGuildAsync(g);
            if (guild.Prefixes.Count > 0)
            {
                prefixes.AddRange(guild.Prefixes.Select(x => new StringPrefix(x)));
            }

            return prefixes;
        }
    }
}