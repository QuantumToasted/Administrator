using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.Extensions.Configuration;

namespace Administrator.Commands
{
    public sealed class AdminPrefixProvider : IPrefixProvider
    {
        private readonly IPrefix _defaultPrefix;

        public AdminPrefixProvider(IConfiguration configuration)
        {
            _defaultPrefix = new StringPrefix(configuration["DEFAULT_PREFIX"]);
        }
        
        // TODO: fill out db stuff
        public ValueTask<IEnumerable<IPrefix>> GetPrefixesAsync(IGatewayUserMessage message)
        {
            return new(new[] {_defaultPrefix});
        }
    }
}