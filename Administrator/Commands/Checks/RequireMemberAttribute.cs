using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireMemberAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx)
        {
            var guild = (SocketGuild) argument;
            var context = (AdminCommandContext) ctx;

            return guild.GetUser(context.User.Id) is null
                ? CheckResult.Unsuccessful(context.Localize("requiremember", Format.Bold(guild.Name)))
                : CheckResult.Successful;
        }
    }
}