using Quartz;

namespace Administrator.Core;

public interface IReschedulableJob : IJob
{
    ValueTask Reschedule(IJobExecutionContext context, CancellationToken cancellationToken = default);
}