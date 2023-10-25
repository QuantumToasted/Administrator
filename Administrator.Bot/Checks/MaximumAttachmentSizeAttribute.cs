using Disqord;
using Disqord.Bot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Bot;

public enum FileSizeMeasure : long
{
    KB = 1_000,
    MB = 1_000_000
}

public class MaximumAttachmentSizeAttribute(double value, FileSizeMeasure measure) : DiscordParameterCheckAttribute
{
    private static AttachmentService? _attachmentService;

    public override bool CanCheck(IParameter parameter, object? value)
        => value is IAttachment;

    public override async ValueTask<IResult> CheckAsync(IDiscordCommandContext context, IParameter parameter, object? argument)
    {
        _attachmentService ??= context.Services.GetRequiredService<AttachmentService>();

        var attachment = (IAttachment) argument!;
        var sizeinBytes = (long)(value * (long)measure);
        if (!await _attachmentService.CheckSizeAsync(attachment.Url, sizeinBytes))
            return Results.Failure($"The provided file must be {value:F}{measure} or smaller in size.");

        return Results.Success;
    }
}