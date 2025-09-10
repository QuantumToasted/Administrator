using Disqord;
using Disqord.Bot.Commands;
using Humanizer.Bytes;
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
    private static AttachmentService? _attachments;

    public override bool CanCheck(IParameter parameter, object? value)
        => value is IAttachment;

    public override async ValueTask<IResult> CheckAsync(IDiscordCommandContext context, IParameter parameter, object? argument)
    {
        _attachments ??= context.Services.GetRequiredService<AttachmentService>();

        var attachment = (IAttachment) argument!;
        var size = measure switch
        {
            FileSizeMeasure.KB => ByteSize.FromKilobytes(value),
            FileSizeMeasure.MB => ByteSize.FromMegabytes(value),
            _ => throw new ArgumentOutOfRangeException(nameof(measure), measure, null)
        };
        
        var cachedAttachment = await _attachments.GetAttachment(attachment);
        if (cachedAttachment.Size > size)
            return Results.Failure($"The provided file must be {size} or smaller in size.");

        return Results.Success;
    }
}