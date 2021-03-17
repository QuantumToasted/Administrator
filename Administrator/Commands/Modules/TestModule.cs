using System;
using Administrator.Database;
using Disqord.Bot;
using Disqord.Gateway;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class TestModule : DiscordGuildModuleBase
    {
        public AdminDbContext Database { get; set; }

        [Command("test")]
        public DiscordCommandResult Test()
        {
            if (Context.Channel is null)
                return Reply("Context.Channel is null");

            return Reply(Context.Channel.Tag);
        }
    }
}