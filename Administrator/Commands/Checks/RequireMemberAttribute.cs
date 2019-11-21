using System;
using System.Threading.Tasks;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireMemberAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx)
        {
            var guild = (CachedGuild) argument;
            var context = (AdminCommandContext) ctx;

            return guild.GetMember(context.User.Id) is null
                ? CheckResult.Unsuccessful(context.Localize("requiremember", Markdown.Bold(guild.Name)))
                : CheckResult.Successful;
        }
    }
}