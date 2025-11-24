using System.Collections.Concurrent;
using System.Linq.Expressions;
using Administrator.Core;
using Disqord;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Qommon.Threading;

namespace Administrator.Database;

public static class DbSetExtensions
{
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> Semaphores = new();

    public static Task<int> GetCurrentDemeritPointsAsync(this IQueryable<Punishment> set, Snowflake guildId, Snowflake targetId)
    {
        return set.OfType<Warning>()
                .Where(x => x.GuildId == guildId && x.Target.Id == (ulong) targetId && x.DemeritPoints > 0)
                .SumAsyncEF(x => x.DemeritPointsRemaining);
    }

    public static async Task<Snowflake?> TryGetAsync(this IQueryable<LoggingChannel> set, Snowflake guildId, LogEventType type)
    {
        var loggingChannel = await set.FirstOrDefaultAsyncEF(x => x.GuildId == guildId && x.EventType == type);
        return loggingChannel?.ChannelId;
    }

    public static Task<GuildConfiguration> GetOrCreateAsync2(this Microsoft.EntityFrameworkCore.DbSet<GuildConfiguration> set, Snowflake guildId)
    {
        return set.GetOrCreateAsync(g => g.GuildId == guildId, () => GuildConfiguration.Create(guildId));
    }

    public static Task<User> GetOrCreateAsync(this Microsoft.EntityFrameworkCore.DbSet<User> set, Snowflake userId)
    {
        return set.GetOrCreateAsync(u => u.UserId == userId, () => User.Create(userId));
    }

    public static Task<Member> GetOrCreateAsync(this Microsoft.EntityFrameworkCore.DbSet<Member> set, Snowflake guildId, Snowflake memberId)
    {
        return set.GetOrCreateAsync(m => m.GuildId == guildId && m.UserId == memberId, () => Member.Create(guildId, memberId));
    }

    private static async Task<T> GetOrCreateAsync<T>(this Microsoft.EntityFrameworkCore.DbSet<T> set, Expression<Func<T, bool>> keyQuery, Func<T> createFactory)
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

    /*
    public static Task Increment(this DbSet<EmojiStats> set, Snowflake guildId, Snowflake emojiId)
    {
        return set.Merge(EmojiStats.Create(guildId, emojiId),
            static es => new EmojiStats { Uses = es.Uses + 1 });
    }

    public static Task InsertOrUpdate(this DbSet<Member> set, Snowflake guildId, Snowflake memberId,
        Expression<Func<Member, Member>> setterExpression)
    {
        return set.Merge(
            new Member { GuildId = guildId, UserId = memberId, Blurb = Member.GenerateBlurb(memberId) },
            setterExpression);
    }
    */

    public static Task<int> Merge(this IQueryable<User> table, Snowflake userId, Expression<Func<User, User>> updateSetterExpression)
    {
        return table.Merge(User.Create(userId), updateSetterExpression);
    }

    public static Task<int> Merge(this IQueryable<GuildConfiguration> table, Snowflake guildId,
        Expression<Func<GuildConfiguration, GuildConfiguration>> updateSetterExpression)
    {
        return table.Merge(GuildConfiguration.Create(guildId), updateSetterExpression);
    }
    
    public static Task<int> Merge<TEntity>(this IQueryable<TEntity> table, 
        TEntity insertEntity,
        Expression<Func<TEntity, TEntity>> updateSetterExpression)
        where TEntity : class
    {
        return table.Merge()
            .Using([insertEntity])
            .OnTargetKey()
            .InsertWhenNotMatched()
            .UpdateWhenMatched(AddParameter(updateSetterExpression))
            .MergeAsync();
    }
    
    public static Task<TProjection?> GetValueOrDefault<TProjection>(this IQueryable<User> set, Snowflake userId,
        Expression<Func<User, TProjection?>> projectionExpression)
    {
        return set.GetValueOrDefault(g => g.UserId == userId, projectionExpression);
    }

    public static Task<TProjection?> GetValueOrDefault<TProjection>(this IQueryable<GuildConfiguration> set, Snowflake guildId,
        Expression<Func<GuildConfiguration, TProjection?>> projectionExpression)
    {
        return set.GetValueOrDefault(g => g.GuildId == guildId, projectionExpression);
    }
    
    private static async Task<TProjection?> GetValueOrDefault<TEntity, TProjection>(this IQueryable<TEntity> set,
        Expression<Func<TEntity, bool>> whereExpression,
        Expression<Func<TEntity, TProjection?>> projectionExpression)
        where TEntity : class
    {
        var value = await set.Where(whereExpression)
            .Select(projectionExpression)
            .FirstOrDefaultAsyncEF();

        return value;
    }

    private static Expression<Func<T1, T1, T2>> AddParameter<T1, T2>(Expression<Func<T1, T2>> expression)
    {
        var newParameter = Expression.Parameter(typeof(T1));
        return Expression.Lambda<Func<T1, T1, T2>>(expression.Body, expression.Parameters[0], newParameter);
    }
}
