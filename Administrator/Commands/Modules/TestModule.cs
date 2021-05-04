using System.Threading.Tasks;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Commands
{
    public class TestModule : DiscordGuildModuleBase
    {
        public AdminDbContext Database { get; set; }

        [Command("guild")]
        [RequireGuild]
        public async Task<DiscordCommandResult> GetGuildInfoAsync()
        {
            var guild = await Database.GetOrCreateGuildAsync(Context.Guild);
            return Reply(guild.Name);
        }
    }
}