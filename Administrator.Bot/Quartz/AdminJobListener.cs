using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot;

public sealed class AdminJobListener(ILogger<AdminJobListener> logger) : IJobListener
{
    public string Name => nameof(AdminJobListener);

    public ILogger Logger { get; } = logger;

    public async ValueTask JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = new())
    {
        if (context.JobInstance is IAdminJob job && jobException is null)
        {
            try
            {
                Logger.LogDebug("Running post-execution rescheduling for job {Job}.", context.JobDetail.Key);
                await job.Reschedule(context, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to run post-execution rescheduling for job {Job}.", context.JobDetail.Key);
            }
        }
    }
    
    // Unused, for now
    ValueTask IJobListener.JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask IJobListener.JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}