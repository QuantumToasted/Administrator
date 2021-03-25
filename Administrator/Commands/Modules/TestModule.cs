using System.Threading.Tasks;
using Administrator.Database;
using Disqord.Bot;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class TestModule : DiscordGuildModuleBase
    {
        public AdminDbContext Database { get; set; }

        [Command("guild")]
        [RequireGuild]
        public async Task<DiscordCommandResult> GetGuildInfoAsync()
        {
            var guild = await Database.GetOrCreateGuildAsync(Context.Guild);
            return Reply(guild.Name);
        }

        [Command("test")]
        public DiscordCommandResult Test(int test)
            => Reply(test.ToString());
    }
}