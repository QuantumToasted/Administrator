using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequirePermissionsPlusAttribute : DiscordGuildCheckAttribute
{
    public override ValueTask<IResult> CheckAsync(IDiscordGuildCommandContext context)
    {
        throw new NotImplementedException();
    }
}