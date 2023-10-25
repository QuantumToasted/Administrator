using Disqord;
using Disqord.Bot.Commands;
using IResult = Qmmands.IResult;
using Results = Qmmands.Results;

namespace Administrator.Bot;

public sealed class RequireInitialAuthorPermissionsAttribute(Permissions permissions) : RequireAuthorPermissionsAttribute(permissions)
{
    // TODO: Not a fan of this check, but it allows flexibility for server owners
    public override ValueTask<IResult> CheckAsync(IDiscordCommandContext context)
        => Results.Success;
}