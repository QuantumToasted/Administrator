using Administrator.Bot.Jobs;
using Administrator.Database;
using Qommon;
using Quartz;
using Quartz.Simpl;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

public static class QuartzExtensions
{
    /*
    private static JobKey GenerateJobKey<TJob>()
        where TJob : IAdminJob
    {
        return JobKey.Create(Guid.NewGuid().ToString(), typeof(TJob).Name);
    }
    
    private static JobKey GenerateJobKey<TJob, TEntity>(TEntity entity)
        where TJob : IAdminJob
        where TEntity : class, INumberKeyedDbEntity
    {
        return JobKey.Create(entity.Id.ToString(), typeof(TJob).Name);
    }

    private static TriggerKey GenerateTriggerKey<TJob>(DateTimeOffset startAt)
        where TJob : IAdminJob
    {
        return new TriggerKey(startAt.ToUnixTimeMilliseconds().ToString(), $"{typeof(TJob)}_Trigger");
    }
    
    private static TriggerKey GenerateTriggerKey<TJob, TEntity>(TEntity entity)
        where TJob : IAdminJob
        where TEntity : class, IExpiringDbEntity, INumberKeyedDbEntity
    {
        Guard.IsNotNull(entity.ExpiresAt);
        
        return new TriggerKey(entity.ExpiresAt.Value.ToUnixTimeMilliseconds().ToString(), $"{typeof(TJob)}_Trigger");
    }
    
    private static TriggerKey GenerateTriggerKey<TJob, TEntity>(TEntity entity, DateTimeOffset startAt)
        where TJob : IAdminJob
        where TEntity : class, INumberKeyedDbEntity
    {
        return new TriggerKey(startAt.ToUnixTimeMilliseconds().ToString(), $"{typeof(TJob)}_Trigger");
    }
    */
    
    public static ValueTask<bool> DeleteAdminJob<TJob, TEntity>(this IScheduler scheduler, TEntity entity)
        where TJob : IAdminJob<TJob, TEntity>
        where TEntity : class, INumberKeyedDbEntity
    {
        return scheduler.DeleteJob(TJob.FormatJobKey(entity));
    }
    
    public static ValueTask<DateTimeOffset> ScheduleAdminJob<TJob>(this IScheduler scheduler, DateTimeOffset startAt, CancellationToken cancellationToken = default)
        where TJob : IAdminJob<TJob>
    {
        return scheduler.ScheduleJob(
            JobBuilder.Create<TJob>().WithIdentity(TJob.FormatJobKey()).Build(),
            TriggerBuilder.Create().WithIdentity(TJob.FormatTriggerKey(startAt)).StartAt(startAt).Build(),
            cancellationToken);
    }

    // TODO: non-expiring entity overload?
    public static ValueTask<DateTimeOffset> ScheduleAdminJob<TJob, TEntity>(this IScheduler scheduler, TEntity entity, CancellationToken cancellationToken = default)
        where TJob : IAdminJob<TJob, TEntity>
        where TEntity : class, IExpiringDbEntity, INumberKeyedDbEntity
    {
        Guard.IsNotNull(entity.ExpiresAt);
        
        return scheduler.ScheduleJob(
            JobBuilder.Create<TJob>().WithIdentity(TJob.FormatJobKey(entity)).UsingJobData("key", entity.Id).Build(),
            TriggerBuilder.Create().WithIdentity(TJob.FormatTriggerKey(entity, entity.ExpiresAt.Value)).StartAt(entity.ExpiresAt.Value).Build(),
            cancellationToken);
    }

    public static ValueTask<DateTimeOffset> SchedulePunishmentExpiryAsync(this IScheduler scheduler, Punishment punishment, CancellationToken cancellationToken = default)
    {
        Guard.IsAssignableToType<IExpiringDbEntity>(punishment);

        return punishment switch
        {
            Ban ban => ScheduleInternal(scheduler, ban, cancellationToken),
            Block block => ScheduleInternal(scheduler, block, cancellationToken),
            TimedRole timedRole => ScheduleInternal(scheduler, timedRole, cancellationToken),
            Timeout timeout => ScheduleInternal(scheduler, timeout, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        };
        
        static ValueTask<DateTimeOffset> ScheduleInternal<TPunishment>(IScheduler scheduler, TPunishment punishment, CancellationToken cancellationToken)
            where TPunishment : RevocablePunishment, IExpiringDbEntity
        {
            return scheduler.ScheduleAdminJob<PunishmentExpiryJob<TPunishment>, TPunishment>(punishment, cancellationToken);
        }
    }
    
    public static ValueTask<DateTimeOffset?> RescheduleAdminJob<TJob, TEntity>(this IScheduler scheduler, TriggerKey oldKey, TEntity entity, CancellationToken cancellationToken = default)
        where TJob : IAdminJob<TJob, TEntity>
        where TEntity : class, INumberKeyedDbEntity, IExpiringDbEntity
    {
        Guard.IsNotNull(entity.ExpiresAt);

        return scheduler.RescheduleJob(
            oldKey,
            TriggerBuilder.Create().WithIdentity(TJob.FormatTriggerKey(entity, entity.ExpiresAt.Value)).StartAt(entity.ExpiresAt.Value).Build(),
            cancellationToken);
    }

    public static ValueTask<DateTimeOffset> ScheduleDemeritPointExpiryJob(this IScheduler scheduler, Warning warning, Member member, CancellationToken cancellationToken = default)
    {
        Guard.IsGreaterThan(warning.DemeritPointsRemaining, 0);
        Guard.IsNotNull(member.NextDemeritPointDecay);

        return ScheduleInternal<DemeritPointDecayJob>(scheduler, warning, member, cancellationToken);

        static ValueTask<DateTimeOffset> ScheduleInternal<TJob>(IScheduler scheduler, Warning warning, Member member, CancellationToken cancellationToken)
            where TJob : IAdminJob<DemeritPointDecayJob, Warning>
        {
            return scheduler.ScheduleJob(
                JobBuilder.Create<DemeritPointDecayJob>().WithIdentity(TJob.FormatJobKey(warning)).UsingJobData("key", warning.Id).Build(),
                TriggerBuilder.Create().WithIdentity(TJob.FormatTriggerKey(warning, member.NextDemeritPointDecay!.Value)).StartAt(member.NextDemeritPointDecay.Value).Build(),
                cancellationToken);
        }
    }

    public static ValueTask<DateTimeOffset?> RescheduleDemeritPointExpiryJob(this IScheduler scheduler, TriggerKey oldKey, Warning warning, Member member, CancellationToken cancellationToken = default)
    {
        Guard.IsGreaterThan(warning.DemeritPointsRemaining, 0);
        Guard.IsNotNull(member.NextDemeritPointDecay);

        return RescheduleInternal<DemeritPointDecayJob>(scheduler, oldKey, warning, member, cancellationToken);

        static ValueTask<DateTimeOffset?> RescheduleInternal<TJob>(IScheduler scheduler, TriggerKey oldKey, Warning warning, Member member, CancellationToken cancellationToken)
            where TJob : IAdminJob<DemeritPointDecayJob, Warning>
        {
            return scheduler.RescheduleJob(oldKey,
                TriggerBuilder.Create().WithIdentity(TJob.FormatTriggerKey(warning, member.NextDemeritPointDecay!.Value)).StartAt(member.NextDemeritPointDecay.Value).Build(),
                cancellationToken);
        }
    }
    
    /*
    public static ValueTask<DateTimeOffset> ScheduleAdminJob<TJob>(this IScheduler scheduler, DateTimeOffset startAt, CancellationToken cancellationToken = default)
        where TJob : AdminJob2
    {
        return scheduler.ScheduleJob(JobBuilder.Create<TJob>().WithIdentity(AdminJob2.GenerateKey<TJob>()).Build(), TriggerBuilder.Create().StartAt(startAt).Build(), cancellationToken);
    }
    
    public static ValueTask<DateTimeOffset> ScheduleAdminJob<TJob, TEntity>(this IScheduler scheduler, TEntity entity, DateTimeOffset startAt, CancellationToken cancellationToken = default)
        where TJob : AdminJob2<TEntity>
        where TEntity : INumberKeyedDbEntity
    {
        return scheduler.ScheduleJob(JobBuilder.Create<TJob>().WithIdentity(AdminJob2.GenerateKey<TJob, TEntity>(entity)).UsingJobData("key", entity.Id.ToString()).Build(), TriggerBuilder.Create().StartAt(startAt).Build(), cancellationToken);
    }
    
    public static ValueTask<DateTimeOffset> ScheduleAdminJob<TJob, TEntity>(this IScheduler scheduler, TEntity entity, CancellationToken cancellationToken = default)
        where TJob : AdminJob2<TEntity>
        where TEntity : IExpiringDbEntity, INumberKeyedDbEntity
    {
        Guard.IsNotNull(entity.ExpiresAt);

        return scheduler.ScheduleAdminJob<TJob, TEntity>(entity, entity.ExpiresAt.Value, cancellationToken);
    }
    
    */
}