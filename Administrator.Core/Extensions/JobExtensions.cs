using System.Globalization;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Core;

internal static partial class JobExtensions
{
    extension<TJob, TEntity, TKey>(TJob job)
        where TJob : class, IAdminJob<TJob, TEntity, TKey>
        where TEntity : class, INumberKeyedEntity<TKey>
        where TKey : INumber<TKey>
    {
        public async ValueTask Reschedule(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            TEntity entity;
        
            try
            {
                var rawKey = context.MergedJobDataMap.GetString("key");
                var entityKey = TKey.Parse(rawKey, CultureInfo.InvariantCulture);
                entity = await job.GetEntity(context, entityKey);
            }
            catch (Exception ex)
            {
                LogGetEntityReschedulingFailure(job.Logger, context.JobDetail.Key);
                throw new JobExecutionException(ex);
            }

            await job.Reschedule(context, entity, cancellationToken);
        }
    }
    
    extension<TJob, TEntity, TKey>(TJob job) 
        where TJob : class, IEntityCoupledJob<TJob, TEntity, TKey>
        where TEntity : class, INumberKeyedEntity<TKey>
        where TKey : INumber<TKey>
    {
        public async ValueTask Execute(IJobExecutionContext context)
        {
            TEntity entity;
        
            try
            {
                var rawKey = context.MergedJobDataMap.GetString("key");
                var entityKey = TKey.Parse(rawKey, CultureInfo.InvariantCulture);
            
                entity = await job.GetEntity(context, entityKey);
            }
            catch (Exception ex)
            {
                LogGetEntityExecutionFailure(job.Logger, context.JobDetail.Key);
                throw new JobExecutionException(ex);
            }

            await job.Execute(context, entity);
        }
    }
    
    [LoggerMessage(LogLevel.Error, "Failed to get the entity for executing job {Job}.")]
    public static partial void LogGetEntityExecutionFailure(ILogger logger, JobKey job);
    
    [LoggerMessage(LogLevel.Error, "Failed to get the entity for rescheduling job {Job}.")]
    public static partial void LogGetEntityReschedulingFailure(ILogger logger, JobKey job);
}