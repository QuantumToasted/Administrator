using Disqord;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public sealed class RequireGrantableOrRevocableAttribute : DiscordGuildParameterCheckAttribute
{
    public override bool CanCheck(IParameter parameter, object? value)
        => value is IRole;

    public override ValueTask<IResult> CheckAsync(IDiscordGuildCommandContext context, IParameter parameter, object? argument)
    {
        var role = (IRole) argument!;
        return role.CanBeGrantedOrRevoked()
            ? Results.Success
            : Results.Failure("This role is a booster role, or otherwise cannot be granted/revoked to/from users.");
    }
}