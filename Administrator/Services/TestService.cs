using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Hosting;
using Microsoft.Extensions.Logging;

namespace Administrator.Services
{
    public sealed class TestService : DiscordClientService
    {
        public TestService(ILogger<TestService> logger, DiscordClientBase client) 
            : base(logger, client)
        {
            client.MessageReceived += HandleMessageReceived;
        }

        private Task HandleMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Logger.LogInformation("Hello, Nhyv! From message ID {MessageId}.", e.MessageId.RawValue);
            return Task.CompletedTask;
        }
    }
}