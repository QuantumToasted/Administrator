using System;
using Disqord.Bot;
using Disqord.Gateway;

namespace Administrator.Commands
{
    public sealed class AdminCommandContext : DiscordCommandContext
    {
        public AdminCommandContext(DiscordBotBase bot, IPrefix prefix, IGatewayUserMessage message, IServiceProvider services) 
            : base(bot, prefix, message, services)
        { }
    }
}