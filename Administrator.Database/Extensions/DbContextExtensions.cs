using System.Collections.Concurrent;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Qommon.Threading;

namespace Administrator.Database;

public static class DbContextExtensions
{
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> Semaphores = new();
    
    public static Task<Guild> GetOrCreateGuildConfigAsync(this AdminDbContext db, Snowflake guildId)
        => db.GetOrCreateAsync(() => db.Guilds.FindAsync(guildId).AsTask(), () => new Guild(guildId));

    public static Task<GlobalUser> GetOrCreateGlobalUserAsync(this AdminDbContext db, Snowflake userId)
        => db.GetOrCreateAsync(() => db.GlobalUsers.FindAsync(userId).AsTask(), () => new GlobalUser(userId));
    
    public static Task<GuildUser> GetOrCreateGuildUserAsync(this AdminDbContext db, Snowflake guildId, Snowflake memberId)
        => db.GetOrCreateAsync(() => db.GuildUsers.FindAsync(guildId, memberId).AsTask(), () => new GuildUser(memberId, guildId));

    public static Task<EmojiStats> GetOrCreateEmojiStatisticsAsync(this AdminDbContext db, Snowflake guildId, Snowflake emojiId)
        => db.GetOrCreateAsync(() => db.EmojiStats.FindAsync(emojiId).AsTask(), () => new EmojiStats(emojiId, guildId));
    
    private static async Task<T> GetOrCreateAsync<T>(this DbContext db, Func<Task<T?>> getFactory, Func<T> createFactory)
        where T : class
    {
        using var _ = await Semaphores.GetOrAdd(typeof(T), _ => new SemaphoreSlim(1, 1)).EnterAsync();
        
        var dbValue = await getFactory.Invoke();
        if (dbValue is not null)
            return dbValue;

        var newValue = createFactory();
        db.Add(newValue!);
        await db.SaveChangesAsync();
        return newValue;
    }
}
