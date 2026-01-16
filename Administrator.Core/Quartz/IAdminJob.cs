using System.Numerics;
using Quartz;

namespace Administrator.Core;

public interface IAdminJob<TJob, TEntity, in TKey> : IEntityCoupledJob<TJob, TEntity, TKey>, IFormattableJob<TJob>, IReschedulableJob
    where TJob : class, IAdminJob<TJob, TEntity, TKey>
    where TEntity : class, INumberKeyedEntity<TKey>
    where TKey : INumber<TKey>
{
    ValueTask Reschedule(IJobExecutionContext context, TEntity entity, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask IReschedulableJob.Reschedule(IJobExecutionContext context, CancellationToken cancellationToken) => ((TJob)this).Reschedule<TJob, TEntity, TKey>(context, cancellationToken);
}

public interface IAdminJob<TJob, TEntity> : IAdminJob<TJob, TEntity, int>
    where TJob : class, IAdminJob<TJob, TEntity, int>
    where TEntity : class, INumberKeyedEntity<int>;