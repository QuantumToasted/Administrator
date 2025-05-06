using Administrator.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot.Jobs;

public sealed class DemeritPointDecayJob(ILogger<DemeritPointDecayJob> logger, QuartzService quartz, AdminDbContext db) : IAdminJob<DemeritPointDecayJob, Warning>
{
    public ILogger Logger { get; } = logger;
    
    public async ValueTask<Warning> GetEntity(IJobExecutionContext context, int warningId)
    {
        var warning = await db.Punishments.OfType<Warning>().FirstAsync(x => x.Id == warningId);
        return warning;
    }

    // we are ONLY decaying demerit points for the warning. Let re-scheduling determine the next decay.
    public async ValueTask Execute(IJobExecutionContext context, Warning warning)
    {
        warning.DemeritPointsRemaining = Math.Max(0, warning.DemeritPointsRemaining - 1);
        await db.SaveChangesAsync();
    }

    public async ValueTask Reschedule(IJobExecutionContext context, Warning warning, CancellationToken cancellationToken)
    {
        Warning? nextWarning = null;
        if (warning.DemeritPointsRemaining == 0)
        {
            // get the next available warning that can decay
            nextWarning = await db.Punishments.OfType<Warning>()
                .Where(x => x.GuildId == warning.GuildId && x.Target.Id == warning.Target.Id)
                .Where(x => x.Id != warning.Id && x.RevokedAt == null && x.DemeritPointsRemaining > 0)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        
        var member = await db.Members.GetOrCreateAsync(warning.GuildId, warning.Target.Id);
        if (nextWarning is null && warning.DemeritPointsRemaining == 0)
        {
            Logger.LogDebug("Member [{GuildId},{UserId}] has reached 0 demerit points. Not rescheduling warning decay.", warning.GuildId.RawValue, warning.Target.Id);

            member.NextDemeritPointDecay = null;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var guild = await db.Guilds.GetOrCreateAsync(warning.GuildId);

        if (guild.DemeritPointDecayInterval is not { } decayInterval)
        {
            Logger.LogWarning("Guild [{GuildId}] has removed their decay interval. Not rescheduling warning decay.", warning.GuildId.RawValue);
            return; // Don't set their decay to null
        }
        
        var nextDecay = member.NextDemeritPointDecay + decayInterval;
        if (nextWarning?.CreatedAt < nextDecay)
        {
            // Note to self: this is an exceptional case that may only result if I am in the process of fixing broken decay.
            nextDecay = nextWarning.CreatedAt + decayInterval;
            
            Logger.LogDebug("Found a warning newer than member [{GuildId},{UserId}]'s next decay. Bumping it to {StartAt}.",
                nextWarning.GuildId.RawValue, nextWarning.Target.Id, nextDecay);
        }
        
        // TODO: this will be set AFTER any modifications
        member.NextDemeritPointDecay = nextDecay;
        await db.SaveChangesAsync(cancellationToken);

        await quartz.RescheduleDemeritPointDecayJobAsync(nextWarning ?? warning, member);
    }

    public static JobKey FormatJobKey(Warning entity)
    {
        return JobKey.Create($"{entity.GuildId}:{entity.Target.Id}", nameof(DemeritPointDecayJob));
    }
}