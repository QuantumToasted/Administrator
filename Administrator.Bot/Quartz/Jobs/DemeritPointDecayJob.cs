using Administrator.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot.Jobs;

public sealed class DemeritPointDecayJob(ILogger<DemeritPointDecayJob> logger, AdminDbContext db) : IAdminJob<DemeritPointDecayJob, Warning>
{
    private int _totalDemeritPoints = -1;
    
    public ILogger Logger { get; } = logger;
    
    public async ValueTask<Warning> GetEntity(IJobExecutionContext context, int warningId)
    {
        var warning = await db.Punishments.OfType<Warning>().FirstAsync(x => x.Id == warningId);
        _totalDemeritPoints = await db.Punishments.GetCurrentDemeritPointsAsync(warning.GuildId, warning.Target.Id);
        return warning;
    }

    public async ValueTask Execute(IJobExecutionContext context, Warning warning)
    {
        var guild = await db.Guilds.GetOrCreateAsync(warning.GuildId);
        
        if (guild.DemeritPointsDecayInterval is not { } interval)
        {
            Logger.LogWarning("Guild {GuildId} does not have a demerit point decay interval, but the decay job fired anyways.", warning.GuildId.RawValue);
            return;
        }

        if (warning.DemeritPointsRemaining == 0)
        {
            Logger.LogWarning("Warning {Id} for member [{GuildId},{UserId}] DemeritPointsRemaining == 0 - was likely revoked.", warning.Id, warning.GuildId.RawValue, warning.Target.Id);
            _totalDemeritPoints = await db.Punishments.GetCurrentDemeritPointsAsync(warning.GuildId, warning.Target.Id);
        }
        else
        {
            warning.DemeritPointsRemaining -= 1; // could use --
            _totalDemeritPoints -= 1;
        }
        
        var member = await db.Members.GetOrCreateAsync(warning.GuildId, warning.Target.Id);

        if (_totalDemeritPoints > 0) 
        {
            member.NextDemeritPointDecay = (member.NextDemeritPointDecay ?? DateTimeOffset.UtcNow) + interval;
        }
        else // 1 -> 0 total points (final warning)
        {
            member.NextDemeritPointDecay = null;
        }

        await db.SaveChangesAsync();

        var currentPoints = await db.Punishments.GetCurrentDemeritPointsAsync(warning.GuildId, warning.Target.Id);
        Logger.LogDebug("Member [{GuildId},{UserId}] has decayed to {Points} demerit points. Next decay: {NextDecay}",
            warning.GuildId.RawValue, warning.Target.Id, currentPoints, member.NextDemeritPointDecay);
    }

    public async ValueTask Reschedule(IJobExecutionContext context, Warning warning, CancellationToken cancellationToken)
    {
        if (_totalDemeritPoints == -1)
        {
            Logger.LogError("_totalDemeritPoints was -1! State was not preserved.");
            return;
        }

        if (_totalDemeritPoints == 0) // 1 -> 0 total points (final warning)
        {
            Logger.LogDebug("_totalDemeritPoints was 0. Not rescheduling due to no more points to decay.");
            return;
        }
        
        var member = await db.Members.GetOrCreateAsync(warning.GuildId, warning.Target.Id);
        if (!member.NextDemeritPointDecay.HasValue)
        {
            Logger.LogWarning("Member [{GuildId},{UserId}] does not have a next decay time, even though they should.", warning.GuildId.RawValue, warning.Target.Id);
            return;
        }

        if (warning.DemeritPointsRemaining == 0)
        {
            // schedule for the next warning
            var nextWarning = await db.Punishments.OfType<Warning>()
                .OrderByDescending(x => x.Id)
                .Where(x => x.DemeritPointsRemaining > 0 && x.RevokedAt == null && x.Target.Id == warning.Target.Id && x.GuildId == warning.GuildId)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextWarning is null)
            {
                Logger.LogDebug("No warnings left to decay for member [{GuildId},{UserId}], not scheduling a decay job.", warning.GuildId.RawValue, warning.Target.Id);
            }
            else
            {
                await context.Scheduler.ScheduleDemeritPointExpiryJob(nextWarning, member, cancellationToken);
            }
            
            return;
        }

        await context.Scheduler.RescheduleDemeritPointExpiryJob(context.Trigger.Key, warning, member, cancellationToken);
    }
}