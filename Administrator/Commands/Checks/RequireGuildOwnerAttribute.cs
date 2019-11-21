using System;
using System.Threading.Tasks;
using Administrator.Common;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequireGuildOwnerAttribute : RequireContextAttribute
    {
        public RequireGuildOwnerAttribute() 
            : base(ContextType.Guild)
        { }

        public override async ValueTask<CheckResult> CheckAsync(CommandContext ctx)
        {
            var baseResult = await base.CheckAsync(ctx);
            if (!baseResult.IsSuccessful)
                return baseResult;

            var context = (AdminCommandContext) ctx;
            return ((CachedMember) context.User).Id != context.Guild.OwnerId
                ? CheckResult.Unsuccessful(context.Localize("requireguildowner"))
                : CheckResult.Successful;
        }
    }
}