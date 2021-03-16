using System;
using Administrator.Database;
using Disqord.Bot;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class TestModule : DiscordModuleBase
    {
        public AdminDbContext Database { get; set; }
        
        [Command("throw")]
        public DiscordCommandResult Throw([Remainder] string message = null)
            => throw new Exception(message ?? "idk");
    }
}