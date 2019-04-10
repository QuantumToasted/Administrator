using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireMemberAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx, IServiceProvider provider)
        {
            var guild = (SocketGuild) argument;
            var context = (AdminCommandContext) ctx;

            return !(guild.GetUser(context.User.Id) is SocketUser _)
                ? CheckResult.Unsuccessful(context.Localize("requiremember", guild.Name))
                : CheckResult.Successful;
        }
    }
}