using Administrator.Database;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public sealed class RequireTagOwnerAttribute : DiscordGuildParameterCheckAttribute
{
    public override bool CanCheck(IParameter parameter, object? value)
        => value is Tag;

    public override ValueTask<IResult> CheckAsync(IDiscordGuildCommandContext context, IParameter parameter, object? argument)
    {
        var tag = (Tag)argument!;
        return tag.OwnerId == context.AuthorId
            ? Results.Success
            : Results.Failure($"You don't own the tag \"{tag.Name}\"!");
    }
}