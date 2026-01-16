using System.Numerics;
using Quartz;

namespace Administrator.Core;

public interface IEntityCoupledJob<TJob, TEntity, in TKey> : ILoggingJob
    where TJob : class, IEntityCoupledJob<TJob, TEntity, TKey>
    where TEntity : class, INumberKeyedEntity<TKey>
    where TKey : INumber<TKey>
{
    ValueTask<TEntity> GetEntity(IJobExecutionContext context, TKey entityKey);
    ValueTask Execute(IJobExecutionContext context, TEntity entity);
    ValueTask IJob.Execute(IJobExecutionContext context) => ((TJob) this).Execute<TJob, TEntity, TKey>(context);
}