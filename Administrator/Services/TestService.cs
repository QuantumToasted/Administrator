using Disqord;
using Disqord.Hosting;
using Microsoft.Extensions.Logging;

namespace Administrator.Services
{
    public sealed class TestService : DiscordClientService
    {
        public TestService(ILogger<TestService> logger, DiscordClientBase client) 
            : base(logger, client)
        { }
    }
}