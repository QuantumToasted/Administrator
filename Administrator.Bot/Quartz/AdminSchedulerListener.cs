using Humanizer;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot;

public class AdminSchedulerListener(ILogger<AdminSchedulerListener> logger) : ISchedulerListener
{
    public ILogger Logger { get; } = logger;
    
    public ValueTask JobScheduled(ITrigger trigger, CancellationToken cancellationToken = new())
    {
        var startAt = trigger.GetNextFireTimeUtc() ?? trigger.StartTimeUtc;
        
        var now = DateTimeOffset.UtcNow;

        Logger.LogTrace("Job {Job} scheduled to fire at {Time} (about {Expires}{Filler}).", trigger.JobKey, startAt, 
            (startAt - now).Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year), startAt < now ? " in the past" : string.Empty);
        
        return ValueTask.CompletedTask;
    }

    ValueTask ISchedulerListener.JobUnscheduled(TriggerKey triggerKey, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.TriggerFinalized(ITrigger trigger, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.TriggerPaused(TriggerKey triggerKey, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.TriggersPaused(string? triggerGroup, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.TriggerResumed(TriggerKey triggerKey, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.TriggersResumed(string? triggerGroup, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.JobAdded(IJobDetail jobDetail, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.JobDeleted(JobKey jobKey, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.JobPaused(JobKey jobKey, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.JobInterrupted(JobKey jobKey, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.JobsPaused(string jobGroup, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.JobResumed(JobKey jobKey, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.JobsResumed(string jobGroup, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.SchedulerError(string msg, SchedulerException cause, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.SchedulerInStandbyMode(CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.SchedulerStarted(CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.SchedulerStarting(CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.SchedulerShutdown(CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.SchedulerShuttingdown(CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask ISchedulerListener.SchedulingDataCleared(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}