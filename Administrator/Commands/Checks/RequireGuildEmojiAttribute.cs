using System;
using System.Threading.Tasks;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireGuildEmojiAttribute : RequireCustomEmojiAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx)
        {
            var baseCheck = await base.CheckAsync(argument, ctx);
            if (!baseCheck.IsSuccessful)
                return baseCheck;

            var context = (AdminCommandContext) ctx;
            var emoji = (LocalCustomEmoji) argument;
            return !context.Guild.Emojis.ContainsKey(emoji.Id)
                ? CheckResult.Unsuccessful(context.Localize("require_guild_emoji"))
                : CheckResult.Successful;
        }
    }
}