using NodaTime;
using Quartz;

namespace Administrator.Core;

public interface IFormattableJob<TJob> : IJob
    where TJob : IFormattableJob<TJob>
{
    static virtual JobKey FormatJobKey() => JobKey.Create(Guid.NewGuid().ToString(), typeof(TJob).Name);
    static virtual TriggerKey FormatTriggerKey(Instant startAt) => new(startAt.ToUnixTimeMilliseconds().ToString(), $"{typeof(TJob).Name}_Trigger");
}