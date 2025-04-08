using Administrator.Bot.Jobs;
using Administrator.Database;
using Disqord.Bot.Hosting;
using LinqToDB;
using Microsoft.Extensions.Logging;
using Qommon;
using Quartz;

namespace Administrator.Bot;

public sealed class QuartzService(ISchedulerFactory schedulerFactory) : DiscordBotService
{
    public async Task UnscheduleDemeritPointJobAsync(Warning warning)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        await UnscheduleDemeritPointJob(scheduler, warning, Bot.StoppingToken);
        
        await using (Bot.Services.CreateAsyncScopeWithDatabase(out var db))
        {
            var member = await db.Members.GetOrCreateAsync(warning.GuildId, warning.Target.Id);

            var otherWarning = await db.Punishments.OfType<Warning>()
                .Where(x => x.Target.Id == member.UserId.RawValue && x.GuildId == member.GuildId && x.Id != warning.Id &&
                            x.DemeritPointsRemaining > 0)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (otherWarning is not null)
            {
                await scheduler.ScheduleDemeritPointExpiryJob(otherWarning, member, Bot.StoppingToken);
            }
        }
    }
    
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
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken: stoppingToken);

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        var reminders = await db.Reminders.Where(x => x.CreatedAt != x.ExpiresAt).ToListAsync(stoppingToken);
        foreach (var reminder in reminders)
        {
            await scheduler.ScheduleAdminJob<ReminderExpiryJob, Reminder>(reminder, cancellationToken: stoppingToken);
        }
        
        Logger.LogDebug("Scheduled {Count} reminder expiry jobs.", reminders.Count);

        var rawEntries = await db.Members.Where(x => x.NextDemeritPointDecay != null)
            .Select(member => new
            {
                Member = member,
                Warning = db.Punishments
                    .OfType<Warning>()
                    .OrderByDescending(x => x.Id)
                    .Where(x => x.GuildId == member.GuildId && x.Target.Id == (ulong)member.UserId)
                    .FirstOrDefault(x => x.DemeritPointsRemaining > 0)
            })
            .Where(x => x.Warning != null)
            .ToListAsync(stoppingToken);

        foreach (var entry in rawEntries)
        {
            await scheduler.ScheduleDemeritPointExpiryJob(entry.Warning!, entry.Member, stoppingToken);
        }
        
        Logger.LogDebug("Scheduled {Count} demerit point decay jobs.", rawEntries.Count);

        var punishments = await db.Punishments.OfType<RevocablePunishment>()
            .Where(x => x.RevokedAt == null)
            .ToListAsync(stoppingToken);

        var expiringPunishments = punishments.Where(x => x is IExpiringDbEntity { ExpiresAt: not null }).ToList();
        foreach (var punishment in expiringPunishments)
        {
            await scheduler.SchedulePunishmentExpiryAsync(punishment, Bot.StoppingToken);
        }
        
        Logger.LogDebug("Scheduled {Count} punishment expiry jobs.", expiringPunishments.Count);

        // TODO: Schedule other jobs

        await scheduler.Start(stoppingToken);
    }
    
    private static async ValueTask UnscheduleDemeritPointJob(IScheduler scheduler, Warning warning, CancellationToken cancellationToken)
    {
        await UnscheduleJob<DemeritPointDecayJob, Warning>(scheduler, warning, cancellationToken);
        
        static async ValueTask UnscheduleJob<TJob, TEntity>(IScheduler scheduler, Warning warning, CancellationToken cancellationToken)
            where TJob : IAdminJob<TJob, TEntity>
            where TEntity : RevocablePunishment
        {
            var entity = Guard.IsOfType<TEntity>(warning);
            
            var triggers = await scheduler.GetTriggersOfJob(TJob.FormatJobKey(entity), cancellationToken);

            await scheduler.UnscheduleJobs(triggers.Select(x => x.Key).ToList(), cancellationToken);
        }
    }
}