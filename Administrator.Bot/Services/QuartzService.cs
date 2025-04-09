using System.Collections;
using Administrator.Bot.Jobs;
using Administrator.Database;
using Disqord.Bot.Hosting;
using LinqToDB;
using Microsoft.Extensions.Logging;
using Qommon;
using Quartz;
using Quartz.Impl.Triggers;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

public sealed class QuartzService(ISchedulerFactory schedulerFactory) : DiscordBotService
{
    /*
    public async Task RefreshDemeritPointJobAsync(Warning warning, Member member)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        await using (Bot.Services.CreateAsyncScopeWithDatabase(out var db))
        {
            var otherWarnings = await db.Punishments.OfType<Warning>()
                .Where(x => x.Target.Id == member.UserId.RawValue && x.GuildId == member.GuildId && x.Id != warning.Id &&
                            x.DemeritPointsRemaining > 0)
                .ToListAsync();

            // Unschedule other warning decay jobs
            foreach (var otherWarning in otherWarnings)
            {
                await UnscheduleDemeritPointJob(scheduler, otherWarning, Bot.StoppingToken);
            }
        }
        
        await RefreshInternal<DemeritPointDecayJob, Warning>(scheduler, warning, member, Bot.StoppingToken);
        
        static async ValueTask RefreshInternal<TJob, TEntity>(IScheduler scheduler, Warning warning, Member member, CancellationToken cancellationToken)
            where TJob : IAdminJob<TJob, TEntity>
            where TEntity : RevocablePunishment
        {
            var entity = Guard.IsOfType<TEntity>(warning);
            
            var triggers = await scheduler.GetTriggersOfJob(TJob.FormatJobKey(entity), cancellationToken);

            if (triggers.FirstOrDefault() is { Key: var key })
            {
                // reschedule, there's already a job
                await scheduler.RescheduleDemeritPointExpiryJob(key, warning, member, cancellationToken);
                return;
            }

            await scheduler.ScheduleDemeritPointExpiryJob(warning, member, cancellationToken);
        }
    }
    */
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken: stoppingToken);

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        var reminders = await db.Reminders.Where(x => x.CreatedAt != x.ExpiresAt).ToListAsync(stoppingToken);
        await scheduler.ScheduleAdminJobs<ReminderExpiryJob, Reminder>(reminders);
        Logger.LogDebug("Scheduled {Count} reminder expiry jobs.", reminders.Count);

        var membersWithDecay = await db.Members.Where(x => x.NextDemeritPointDecay != null).ToListAsync(stoppingToken);
        var warningsWithDemeritPoints = await db.Punishments.OfType<Warning>().Where(x => x.DemeritPointsRemaining > 0 && x.RevokedAt == null).ToListAsync(stoppingToken);
        var guilds = await db.Guilds.ToListAsync(stoppingToken);
        var warningsWithDecay = membersWithDecay.Select(member => new
            {
                Member = member,
                Warning = warningsWithDemeritPoints
                    .Where(x => x.GuildId == member.GuildId && x.Target.Id == (ulong)member.UserId)
                    .FirstOrDefault(x => x.DemeritPointsRemaining > 0),
                CanDecay = guilds.Any(x => x.GuildId == member.GuildId && x.DemeritPointsDecayInterval.HasValue)
            })
            .Where(x => x.Warning != null && x.CanDecay)
            .Select(x => (x.Warning!, x.Member.NextDemeritPointDecay!.Value))
            .ToList();

        await scheduler.ScheduleAdminJobs<DemeritPointDecayJob, Warning>(warningsWithDecay);
        
        Logger.LogDebug("Scheduled {Count} demerit point decay jobs.", warningsWithDecay.Count);

        var punishments = await db.Punishments.OfType<RevocablePunishment>()
            .Where(x => x.RevokedAt == null)
            .ToListAsync(stoppingToken);

        var expiringPunishments = punishments.Where(x => x is IExpiringDbEntity { ExpiresAt: not null }).ToList();
        await SchedulePunishmentExpiryJobsAsync(scheduler, expiringPunishments);
        
        Logger.LogDebug("Scheduled {Count} punishment expiry jobs.", expiringPunishments.Count);

        await scheduler.ScheduleJob(
            JobBuilder.Create<BackpackUpdateJob>().WithIdentity(Guid.NewGuid().ToString(), nameof(BackpackUpdateJob)).Build(),
            TriggerBuilder.Create().StartNow().WithSchedule(SimpleScheduleBuilder.Create().WithIntervalInMinutes(30)).Build(),
            Bot.StoppingToken);
        
        // TODO: Schedule other jobs

        await scheduler.Start(stoppingToken);
    }
    
    public async ValueTask<DateTimeOffset> SchedulePunishmentExpiryJobAsync(Punishment punishment)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        
        return await (punishment switch
        {
            Ban ban => ScheduleInternal(scheduler, ban),
            Block block => ScheduleInternal(scheduler, block),
            TimedRole timedRole => ScheduleInternal(scheduler, timedRole),
            Timeout timeout => ScheduleInternal(scheduler, timeout),
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        });
        
        static ValueTask<DateTimeOffset> ScheduleInternal<TPunishment>(IScheduler scheduler, TPunishment punishment)
            where TPunishment : RevocablePunishment, IExpiringDbEntity
        {
            return scheduler.ScheduleAdminJob<PunishmentExpiryJob<TPunishment>, TPunishment>(punishment);
        }
    }

    public async ValueTask ScheduleDemeritPointDecayJobAsync(Warning warning, Member member)
    {
        Guard.IsGreaterThan(warning.DemeritPointsRemaining, 0);
        Guard.IsNotNull(member.NextDemeritPointDecay);
        
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.ScheduleAdminJob<DemeritPointDecayJob, Warning>(warning, member.NextDemeritPointDecay.Value);
    }

    public async ValueTask RescheduleDemeritPointDecayJobAsync(Warning warning, Member member)
    {
        Guard.IsGreaterThan(warning.DemeritPointsRemaining, 0);
        Guard.IsNotNull(member.NextDemeritPointDecay);
        
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.RescheduleAdminJob<DemeritPointDecayJob, Warning>(warning, member.NextDemeritPointDecay.Value);
    }

    // this happens after the warning has been set to 0 demerit points
    public async ValueTask UnscheduleDemeritPointDecayJobAsync(Warning warning)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        if (await scheduler.GetTriggerKey<DemeritPointDecayJob, Warning>(warning) is not { } triggerKey)
            return;
        
        await using (Bot.Services.CreateAsyncScopeWithDatabase(out var db))
        {
            var member = await db.Members.GetOrCreateAsync(warning.GuildId, warning.Target.Id);
            var otherWarning = await db.Punishments
                .OfType<Warning>()
                .Where(x => x.Target.Id == member.UserId.RawValue && 
                            x.GuildId == member.GuildId && 
                            x.Id != warning.Id &&
                            x.RevokedAt == null &&
                            x.DemeritPointsRemaining > 0)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (otherWarning is not null)
            {
                await RescheduleDemeritPointDecayJobAsync(otherWarning, member);
            }
            else
            {
                await scheduler.DeleteAdminJob<DemeritPointDecayJob, Warning>(warning);
            }
        }
    }
    
    private static ValueTask SchedulePunishmentExpiryJobsAsync(IScheduler scheduler, IEnumerable<RevocablePunishment> punishments)
    {
        var jobDict = punishments.Select(FormatJobAndTrigger)
            .ToDictionary(x => x.JobDetail, IReadOnlyCollection<ITrigger> (x) => [x.Trigger]);

        return scheduler.ScheduleJobs(jobDict, true);

        static (IJobDetail JobDetail, ITrigger Trigger) FormatJobAndTrigger(RevocablePunishment punishment)
        {
            return punishment switch
            {
                Ban ban => ban.FormatJobAndTrigger<PunishmentExpiryJob<Ban>, Ban>(),
                Block block => block.FormatJobAndTrigger<PunishmentExpiryJob<Block>, Block>(),
                TimedRole timedRole => timedRole.FormatJobAndTrigger<PunishmentExpiryJob<TimedRole>, TimedRole>(),
                Timeout timeout => timeout.FormatJobAndTrigger<PunishmentExpiryJob<Timeout>, Timeout>(),
                _ => throw new ArgumentOutOfRangeException(nameof(punishment))
            };
        }
    }
}