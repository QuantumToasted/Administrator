using Disqord;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public sealed class RequireHierarchyIfMemberAttribute : DiscordGuildParameterCheckAttribute
{
    public override bool CanCheck(IParameter parameter, object? value)
        => value is IUser;

    public override async ValueTask<IResult> CheckAsync(IDiscordGuildCommandContext context, IParameter parameter, object? argument)
    {
        if (argument is not IMember member)
            return Results.Success;
        
        var botCheck = new RequireBotRoleHierarchyAttribute();
        var botCheckAttributeResult = await botCheck.CheckAsync(context, parameter, member);
        if (!botCheckAttributeResult.IsSuccessful)
            return botCheckAttributeResult;

        var authorCheck = new RequireAuthorRoleHierarchyAttribute();
        var authorCheckAttributeResult = await authorCheck.CheckAsync(context, parameter, member);
        if (!authorCheckAttributeResult.IsSuccessful)
            return authorCheckAttributeResult;

        return Results.Success;
    }
}