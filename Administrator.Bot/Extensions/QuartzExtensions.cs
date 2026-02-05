using Administrator.Core;
using Qommon;
using Quartz;

namespace Administrator.Bot;

public static class QuartzExtensions
{
    public static ValueTask<DateTimeOffset> ScheduleAdminJob<TJob>(this IScheduler scheduler, TimeSpan interval)
        where TJob : IAdminJob<TJob>
    {
        var jobDetail = JobBuilder.Create<TJob>().WithIdentity(TJob.FormatJobKey()).Build();
        var trigger = TriggerBuilder.Create().WithIdentity(TJob.FormatTriggerKey(DateTimeOffset.UtcNow)).WithSimpleSchedule(x => x.WithInterval(interval).RepeatForever()).Build();
        return scheduler.ScheduleJob(jobDetail, trigger);
    }

    public static ValueTask ScheduleAdminJobs<TJob, TEntity>(this IScheduler scheduler, IEnumerable<(TEntity Entity, DateTimeOffset StartAt)> entities)
        where TJob : IAdminJob<TJob, TEntity> 
        where TEntity : class, IKeyedEntity<int>
    {
        var jobDict = entities.Select(static t => t.Entity.FormatJobAndTrigger<TJob, TEntity>(t.StartAt))
            .ToDictionary(t => t.JobDetail, IReadOnlyCollection<ITrigger> (x) => [x.Trigger]);
        
        return scheduler.ScheduleJobs(jobDict, true);
    }
    
    public static ValueTask ScheduleAdminJobs<TJob, TEntity>(this IScheduler scheduler, IEnumerable<TEntity> entities)
        where TJob : IAdminJob<TJob, TEntity> 
        where TEntity : class, IKeyedEntity<int>, IExpiringEntity
    {
        var jobDict = entities.Select(FormatJobAndTrigger<TJob, TEntity>)
            .ToDictionary(x => x.JobDetail, IReadOnlyCollection<ITrigger> (x) => [x.Trigger]);
        
        return scheduler.ScheduleJobs(jobDict, true);
    }
    
    public static ValueTask<DateTimeOffset> ScheduleAdminJob<TJob, TEntity>(this IScheduler scheduler, TEntity entity, DateTimeOffset startAt)
        where TJob : IAdminJob<TJob, TEntity> 
        where TEntity : class, IKeyedEntity<int>
    {
        var (jobDetail, trigger) = entity.FormatJobAndTrigger<TJob, TEntity>(startAt);
        return scheduler.ScheduleJob(jobDetail, trigger);
    }

    public static ValueTask<DateTimeOffset> ScheduleAdminJob<TJob, TEntity>(this IScheduler scheduler, TEntity entity)
        where TJob : IAdminJob<TJob, TEntity> 
        where TEntity : class, IKeyedEntity<int>, IExpiringEntity
    {
        var (jobDetail, trigger) = entity.FormatJobAndTrigger<TJob, TEntity>();
        return scheduler.ScheduleJob(jobDetail, trigger);
    }
    
    public static async ValueTask RescheduleAdminJob<TJob, TEntity>(this IScheduler scheduler, TEntity entity, DateTimeOffset startAt)
        where TJob : IAdminJob<TJob, TEntity> 
        where TEntity : class, IKeyedEntity<int>
    {
        var triggerKey = await scheduler.GetTriggerKey<TJob, TEntity>(entity);
        if (triggerKey is null)
        {
            await scheduler.ScheduleAdminJob<TJob, TEntity>(entity, startAt);
            return;
        }

        var (_, newTrigger) = entity.FormatJobAndTrigger<TJob, TEntity>(startAt);
        await scheduler.RescheduleJob(triggerKey, newTrigger);
    }

    public static async ValueTask RescheduleAdminJob<TJob, TEntity>(this IScheduler scheduler, TEntity entity)
        where TJob : IAdminJob<TJob, TEntity> 
        where TEntity : class, IKeyedEntity<int>, IExpiringEntity
    {
        Guard.IsNotNull(entity.ExpiresAt);

        var triggerKey = await scheduler.GetTriggerKey<TJob, TEntity>(entity);
        if (triggerKey is null)
        {
            await scheduler.ScheduleAdminJob<TJob, TEntity>(entity);
            return;
        }

        var (_, newTrigger) = entity.FormatJobAndTrigger<TJob, TEntity>();
        await scheduler.RescheduleJob(triggerKey, newTrigger);
    }
    
    public static ValueTask<bool> DeleteAdminJob<TJob, TEntity>(this IScheduler scheduler, TEntity entity)
        where TJob : IAdminJob<TJob, TEntity>
        where TEntity : class, IKeyedEntity<int>
    {
        return scheduler.DeleteJob(TJob.FormatJobKey(entity));
    }
    
    public static ValueTask<bool> DeleteAdminJobs<TJob, TEntity>(this IScheduler scheduler, IEnumerable<TEntity> entities)
        where TJob : IAdminJob<TJob, TEntity>
        where TEntity : class, IKeyedEntity<int>
    {
        var jobKeys = entities.Select(TJob.FormatJobKey).ToList();
        return scheduler.DeleteJobs(jobKeys);
    }

    public static async ValueTask<TriggerKey?> GetTriggerKey<TJob, TEntity>(this IScheduler scheduler, TEntity entity)
        where TJob : IAdminJob<TJob, TEntity> 
        where TEntity : class, IKeyedEntity<int>
    {
        var triggers = await scheduler.GetTriggersOfJob(TJob.FormatJobKey(entity));
        return triggers.FirstOrDefault()?.Key;
    }
    
    public static (IJobDetail JobDetail, ITrigger Trigger) FormatJobAndTrigger<TJob, TEntity>(this TEntity entity, DateTimeOffset startAt)
        where TJob : IAdminJob<TJob, TEntity> 
        where TEntity : class, IKeyedEntity<int>
    {
        var jobDetail = JobBuilder.Create<TJob>().WithIdentity(TJob.FormatJobKey(entity)).WithEntityKey(entity).Build();
        var trigger = TriggerBuilder.Create().WithIdentity(TJob.FormatTriggerKey(entity, startAt)).StartAt(startAt).Build();

        return (jobDetail, trigger);
    }
    
    public static (IJobDetail JobDetail, ITrigger Trigger) FormatJobAndTrigger<TJob, TEntity>(this TEntity entity)
        where TJob : IAdminJob<TJob, TEntity> 
        where TEntity : class, IKeyedEntity<int>, IExpiringEntity
    {
        Guard.IsNotNull(entity.ExpiresAt);

        return FormatJobAndTrigger<TJob, TEntity>(entity, entity.ExpiresAt.Value);
    }

    private static JobBuilder WithEntityKey<TEntity>(this JobBuilder builder, TEntity entity)
        where TEntity : class, IKeyedEntity<int>
    {
        return builder.UsingJobData("key", entity.Id);
    }
}