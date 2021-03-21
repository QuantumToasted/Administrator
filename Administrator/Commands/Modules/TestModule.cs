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
        
        public Random Random { get; set; }

        [Command("choose")]
        public DiscordCommandResult Choose(params string[] choices)
        {
            if (choices.Length == 0)
                return Reply("You're actually a retard, you have to supply things to choose from.");

            return Reply(choices[Random.Next(0, choices.Length)]);
        }

        [Command("test")]
        public DiscordCommandResult Test()
        {
            if (Context.Channel is null)
                return Reply("Context.Channel is null");

            return Reply(Context.Channel.Tag);
        }
    }
}