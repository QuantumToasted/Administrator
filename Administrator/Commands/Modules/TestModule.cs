using Disqord.Bot;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class TestModule : AdminModuleBase
    {
        [Command("echo")]
        public DiscordCommandResult Echo([Remainder] string text)
            => Reply(text);

        [Command("add")]
        public DiscordCommandResult Add(int val1, int val2)
            => Reply((val1 + val2).ToString());
    }
}