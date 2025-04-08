using Administrator.Database;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot;

public interface IAdminJob : IJob
{
    ILogger Logger { get; }
    ValueTask Reschedule(IJobExecutionContext context, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}

public interface IAdminJob<TJob> : IAdminJob
    where TJob : IAdminJob<TJob>
{
    static virtual JobKey FormatJobKey() => JobKey.Create(Guid.NewGuid().ToString(), typeof(TJob).Name);
    static virtual TriggerKey FormatTriggerKey(DateTimeOffset startAt) => new(startAt.ToUnixTimeMilliseconds().ToString(), $"{typeof(TJob).Name}_Trigger");
}

public interface IAdminJob<TJob, TEntity> : IAdminJob<TJob>
    where TJob : IAdminJob<TJob, TEntity>
    where TEntity : class, INumberKeyedDbEntity
    //where TEntity : INumberKeyedDbEntity
{
    ValueTask<TEntity> GetEntity(IJobExecutionContext context, int entityKey);
    ValueTask Execute(IJobExecutionContext context, TEntity entity);
    ValueTask Reschedule(IJobExecutionContext context, TEntity entity, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    static virtual JobKey FormatJobKey(TEntity entity) => JobKey.Create(entity.Id.ToString(), typeof(TJob).Name);
    static virtual TriggerKey FormatTriggerKey(TEntity entity, DateTimeOffset startAt) => new(startAt.ToUnixTimeMilliseconds().ToString(), $"{typeof(TJob).Name}_Trigger.{entity.Id}");
    async ValueTask IJob.Execute(IJobExecutionContext context)
    {
        TEntity entity;
        
        try
        {
            var entityKey = context.MergedJobDataMap.GetInt("key");
            entity = await GetEntity(context, entityKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get the entity for executing job {Job}.", context.JobDetail.Key);

            throw new JobExecutionException(ex);
        }

        await Execute(context, entity);
    }
    async ValueTask IAdminJob.Reschedule(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        TEntity entity;
        
        try
        {
            var entityKey = context.MergedJobDataMap.GetInt("key");
            entity = await GetEntity(context, entityKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get the entity for rescheduling job {Job}.", context.JobDetail.Key);

            throw new JobExecutionException(ex);
        }

        await Reschedule(context, entity, cancellationToken);
    }
}