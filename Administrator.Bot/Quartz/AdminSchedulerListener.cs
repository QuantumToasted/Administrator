using Humanizer;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot;

public class AdminSchedulerListener(ILogger<AdminSchedulerListener> logger) : ISchedulerListener
{
    public ILogger Logger { get; } = logger;

    public ValueTask JobScheduled(IScheduler scheduler, ITrigger trigger, CancellationToken cancellationToken = new())
    {
        var startAt = trigger.NextFireTimeUtc ?? trigger.StartTimeUtc;

        var now = DateTimeOffset.UtcNow;

        Logger.LogTrace("Job {Job} scheduled to fire at {Time} (about {Expires}{Filler}).", trigger.JobKey, startAt,
            (startAt - now).Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year),
            startAt < now ? " in the past" : string.Empty);

        return ValueTask.CompletedTask;
    }
}