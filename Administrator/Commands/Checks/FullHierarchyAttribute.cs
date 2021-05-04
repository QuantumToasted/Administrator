using System.Threading.Tasks;
using Disqord.Bot;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class FullHierarchyAttribute : DiscordGuildParameterCheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(object argument, DiscordGuildCommandContext context)
        {
            var botCheck = await new RequireBotHierarchyAttribute().CheckAsync(argument, context);
            if (!botCheck.IsSuccessful)
                return Failure(botCheck.FailureReason);

            return await new RequireAuthorHierarchyAttribute().CheckAsync(argument, context);
        }
    }
}