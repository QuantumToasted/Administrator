using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot.Jobs;

public sealed class AttachmentCleanupJob(AttachmentService attachments, ILogger<AttachmentCleanupJob> logger) : IAdminJob<AttachmentCleanupJob>
{
    public ValueTask Execute(IJobExecutionContext context)
    {
        try
        {
            var count = attachments.ClearOldAttachments();
            
            if (count > 0)
                Logger.LogInformation("Cleared {Count} old attachments.", count);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to clear old attachments.");
        }
        
        return ValueTask.CompletedTask;
    }

    public ILogger Logger { get; } = logger;
}