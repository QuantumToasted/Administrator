using Disqord;
using Disqord.Bot.Commands;
using Humanizer;
using Qmmands;

namespace Administrator.Bot;

[AttributeUsage(AttributeTargets.Parameter)]
public class RequireAttachmentSizeUnderAttribute(ByteSize maximumSize) : DiscordParameterCheckAttribute
{
    public ByteSize MaximumSize { get; } = maximumSize;

    public override bool CanCheck(IParameter parameter, object? value)
        => value is IAttachment;

    public override ValueTask<IResult> CheckAsync(IDiscordCommandContext context, IParameter parameter, object? argument)
    {
        var attachment = (IAttachment)argument!;
        if (attachment.Size > MaximumSize)
        {
            return Results.Failure($"The supplied attachment {Markdown.Code(attachment.FileName)} must be " +
                                   $"under {Markdown.Bold(MaximumSize)} (was {Markdown.Bold(attachment.Size)}).");
        }
        
        return Results.Success;
    }
}