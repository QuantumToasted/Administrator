using Disqord;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

[AttributeUsage(AttributeTargets.Parameter)]
public class RequireAttachmentExtensionsAttribute(params string[] allowedExtensions) : DiscordParameterCheckAttribute
{
    public override bool CanCheck(IParameter parameter, object? value)
        => value is IAttachment;

    public override ValueTask<IResult> CheckAsync(IDiscordCommandContext context, IParameter parameter, object? argument)
    {
        var attachment = (IAttachment) argument!;

        var uri = new Uri(attachment.Url);
        var extension = Path.GetExtension(uri.AbsolutePath);

        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension[1..], StringComparer.InvariantCultureIgnoreCase))
            return Results.Failure($"The supplied URL was not to a file of the following type(s): {string.Join(',', allowedExtensions)}.");

        return Results.Success;
    }
}