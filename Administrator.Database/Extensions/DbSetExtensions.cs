using System.Collections.Concurrent;
using System.Linq.Expressions;
using Administrator.Core;
using Disqord;
using LinqToDB.EntityFrameworkCore;
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
                .Where(x => x.GuildId == guildId && x.Target.Id == (ulong) targetId && x.DemeritPoints > 0)
                .SumAsyncEF(x => x.DemeritPointsRemaining);
    }

    public static async Task<Snowflake?> TryGetAsync(this DbSet<LoggingChannel> set, Snowflake guildId, LogEventType type)
    {
        var loggingChannel = await set.FirstOrDefaultAsyncEF(x => x.GuildId == guildId && x.EventType == type);
        return loggingChannel?.ChannelId;
    }

    public static Task<GuildConfiguration> GetOrCreateAsync(this DbSet<GuildConfiguration> set, Snowflake guildId)
    {
        return set.GetOrCreateAsync(g => g.GuildId == guildId, () => GuildConfiguration.Create(guildId));
    }

    public static Task<User> GetOrCreateAsync(this DbSet<User> set, Snowflake userId)
    {
        return set.GetOrCreateAsync(u => u.UserId == userId, () => User.Create(userId));
    }

    public static Task<Member> GetOrCreateAsync(this DbSet<Member> set, Snowflake guildId, Snowflake memberId)
    {
        return set.GetOrCreateAsync(m => m.GuildId == guildId && m.UserId == memberId, () => Member.Create(guildId, memberId));
    }

    public static Task<EmojiStats> GetOrCreateAsync(this DbSet<EmojiStats> set, Snowflake guildId, Snowflake emojiId)
    {
        return set.GetOrCreateAsync(es => es.GuildId == guildId && es.EmojiId == emojiId, () => EmojiStats.Create(guildId, emojiId));
    }

    private static async Task<T> GetOrCreateAsync<T>(this DbSet<T> set, Expression<Func<T, bool>> keyQuery, Func<T> createFactory)
        where T : class
    {
        using (await Semaphores.GetOrAdd(typeof(T), _ => new SemaphoreSlim(1, 1)).EnterAsync())
        {
            if (await set.FirstOrDefaultAsyncEF(keyQuery) is { } dbValue)
                return dbValue;

            var newValue = createFactory.Invoke();
            set.Add(newValue);
            await set.GetService<ICurrentDbContext>().Context.SaveChangesAsync();
            return newValue;
        }
    }
}
