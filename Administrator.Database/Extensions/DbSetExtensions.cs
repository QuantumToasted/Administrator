using System.Collections.Concurrent;
using System.Linq.Expressions;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Qommon.Threading;

namespace Administrator.Database;

public static class DbSetExtensions
{
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> Semaphores = new();

    public static Task<int> GetCurrentDemeritPointsAsync(this DbSet<Punishment> set, Snowflake guildId, Snowflake targetId)
    {
        return set.AsNoTracking()
            .OfType<Warning>()
            .Where(x => x.GuildId == guildId && x.Target.Id == (ulong) targetId && x.RevokedAt != null)
            .SumAsync(x => x.DemeritPointsRemaining);
    }

    public static Task<LoggingChannel?> TryGetLoggingChannelAsync(this DbSet<LoggingChannel> set, Snowflake guildId, LogEventType type)
        => set.FirstOrDefaultAsync(x => x.GuildId == guildId && x.EventType == type);

    public static Task<Guild> GetOrCreateAsync(this DbSet<Guild> set, Snowflake guildId)
    {
        return set.GetOrCreateAsync(x => x.GuildId == guildId, () => new Guild(guildId)
        {
            /* TODO: should this come back?
            AutomaticPunishments = 
            [
                new AutomaticPunishment(guildId, 5, PunishmentType.Timeout, TimeSpan.FromHours(4)),
                new AutomaticPunishment(guildId, 10, PunishmentType.Timeout, TimeSpan.FromDays(1)),
                new AutomaticPunishment(guildId, 15, PunishmentType.Ban, TimeSpan.FromDays(7)),
                new AutomaticPunishment(guildId, 20, PunishmentType.Ban, null)
            ]
            */
        });
    }

    public static Task<User> GetOrCreateAsync(this DbSet<User> set, Snowflake userId)
        => set.GetOrCreateAsync(x => x.UserId == userId, () => new User(userId));

    public static Task<Member> GetOrCreateAsync(this DbSet<Member> set, Snowflake guildId, Snowflake memberId)
        => set.GetOrCreateAsync(x => x.GuildId == guildId && x.UserId == memberId, () => new Member(guildId, memberId));

    public static Task<EmojiStats> GetOrCreateAsync(this DbSet<EmojiStats> set, Snowflake guildId, Snowflake emojiId)
        => set.GetOrCreateAsync(x => x.GuildId == guildId && x.EmojiId == emojiId, () => new EmojiStats(emojiId, guildId));

    private static async Task<T> GetOrCreateAsync<T>(this DbSet<T> set, Expression<Func<T, bool>> keyQuery, Func<T> createFactory)
        where T : class
    {
        using var _ = await Semaphores.GetOrAdd(typeof(T), _ => new SemaphoreSlim(1, 1)).EnterAsync();
        if (await set.FirstOrDefaultAsync(keyQuery) is { } dbValue)
            return dbValue;

        var newValue = createFactory.Invoke();
        set.Add(newValue);
        await set.GetService<ICurrentDbContext>().Context.SaveChangesAsync();
        return newValue;
    }
    
    /*
    private static async Task<T> GetOrCreateAsync<T>(this DbContext db, Func<Task<T?>> getFactory, Func<T> createFactory)
        where T : class
    {
        using var _ = await Semaphores.GetOrAdd(typeof(T), _ => new SemaphoreSlim(1, 1)).EnterAsync();
        var dbValue = await getFactory.Invoke();
        if (dbValue is not null)
            return dbValue;
        
        var newValue = createFactory();
        db.Add(newValue);
        await db.SaveChangesAsync();
        return newValue;
    }
    */
}
