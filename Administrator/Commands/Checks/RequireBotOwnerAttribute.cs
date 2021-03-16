using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class RequireBotOwnerAttribute : DiscordCheckAttribute
    {
        private static readonly HashSet<Snowflake> OwnerIds = new() {167452465317281793};
        
        public override ValueTask<CheckResult> CheckAsync(DiscordCommandContext context)
        {
            if (!OwnerIds.Contains(context.Author.Id))
                return Failure(string.Empty);

            return Success();
        }
    }
}