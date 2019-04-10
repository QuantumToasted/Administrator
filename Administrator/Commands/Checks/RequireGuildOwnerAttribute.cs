using System;
using System.Threading.Tasks;
using Administrator.Common;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class RequireGuildOwnerAttribute : RequireContextAttribute
    {
        public RequireGuildOwnerAttribute() 
            : base(ContextType.Guild)
        { }

        public override async ValueTask<CheckResult> CheckAsync(CommandContext ctx, IServiceProvider provider)
        {
            var baseResult = await base.CheckAsync(ctx, provider);
            if (!baseResult.IsSuccessful)
                return baseResult;

            var context = (AdminCommandContext) ctx;
            return ((SocketGuildUser) context.User).Id != context.Guild.OwnerId
                ? CheckResult.Unsuccessful(context.Localize("requireguildowner"))
                : CheckResult.Successful;
        }
    }
}