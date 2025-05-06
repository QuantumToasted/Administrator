using System.Collections.Concurrent;
using System.Linq.Expressions;
using Administrator.Core;
using Disqord;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Qommon.Threading;

namespace Administrator.Database;

public static class DbSetExtensions
{
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> Semaphores = new();

    public static Task<int> GetCurrentDemeritPointsAsync(this Microsoft.EntityFrameworkCore.DbSet<Punishment> set, Snowflake guildId, Snowflake targetId)
    {
        return set.AsNoTracking()
                .OfType<Warning>()
                .Where(x => x.GuildId == guildId && x.Target.Id == (ulong) targetId && x.DemeritPoints > 0)
                .SumAsyncEF(x => x.DemeritPointsRemaining);
    }

    public static async Task<Snowflake?> TryGetAsync(this Microsoft.EntityFrameworkCore.DbSet<LoggingChannel> set, Snowflake guildId, LogEventType type)
    {
        var loggingChannel = await set.FirstOrDefaultAsyncEF(x => x.GuildId == guildId && x.EventType == type);
        return loggingChannel?.ChannelId;
    }

    public static Task<GuildConfiguration> GetOrCreateAsync(this Microsoft.EntityFrameworkCore.DbSet<GuildConfiguration> set, Snowflake guildId)
    {
        return set.GetOrCreateAsync(g => g.GuildId == guildId, () => GuildConfiguration.Create(guildId));
        /*
        return set.GetOrCreateAsync(guildId, static g => GuildConfiguration.Create(g), static g => new GuildConfiguration
        {
            GuildId = g.GuildId,
            Settings = g.Settings,
            LevelUpEmoji = g.LevelUpEmoji,
            XpExemptChannelIds = g.XpExemptChannelIds,
            AutoQuoteExemptChannelIds = g.AutoQuoteExemptChannelIds,
            DefaultBanPruneDays = g.DefaultBanPruneDays,
            DefaultWarningDemeritPoints = g.DefaultWarningDemeritPoints,
            DemeritPointDecayInterval = g.DemeritPointDecayInterval,
            MaxLuaCommands = g.MaxLuaCommands
        }, );*/
    }

    public static Task<User> GetOrCreateAsync(this Microsoft.EntityFrameworkCore.DbSet<User> set, Snowflake userId)
    {
        return set.GetOrCreateAsync(u => u.UserId == userId, () => User.Create(userId));
        /*
        return set.GetOrCreateAsync(userId, static u => User.Create(u), static u => new User
        {
            UserId = u.UserId,
            LastLevelUp = u.LastLevelUp,
            LastXpGain = u.LastXpGain,
            ResumeHighlightsAfterMessageCount = u.ResumeHighlightsAfterMessageCount,
            ResumeHighlightsAfterInterval = u.ResumeHighlightsAfterInterval,
            BlacklistedHighlightUserIds = u.BlacklistedHighlightUserIds,
            BlacklistedHighlightChannelIds = u.BlacklistedHighlightChannelIds
        });
        */
    }

    public static Task<Member> GetOrCreateAsync(this Microsoft.EntityFrameworkCore.DbSet<Member> set, Snowflake guildId, Snowflake memberId)
    {
        return set.GetOrCreateAsync(m => m.GuildId == guildId && m.UserId == memberId, () => Member.Create(guildId, memberId));
        /*
        return set.GetOrCreateAsync(guildId, memberId, static (g, m) => Member.Create(g, m), static m => new Member
        {
            GuildId = m.GuildId,
            UserId = m.UserId,
            Blurb = m.Blurb
        });
        */
    }

    public static Task<EmojiStats> GetOrCreateAsync(this Microsoft.EntityFrameworkCore.DbSet<EmojiStats> set, Snowflake guildId, Snowflake emojiId)
    {
        return set.GetOrCreateAsync(es => es.GuildId == guildId && es.EmojiId == emojiId, () => EmojiStats.Create(guildId, emojiId));
        /*
        return set.GetOrCreateAsync(guildId, emojiId, static (g, e) => EmojiStats.Create(g, e), static es => new EmojiStats
        {
            EmojiId = es.EmojiId,
            GuildId = es.GuildId
        });
        */
    }

    /*
    private static Task<TEntity> GetOrCreateAsync<TKey, TEntity>(this Microsoft.EntityFrameworkCore.DbSet<TEntity> set, TKey key, 
        Func<TKey, TEntity> createFactory, Expression<Func<TEntity, TEntity>> setterExpression, Expression<Func<string, TEntity, TEntity, TEntity, TEntity>> mergeExpression)
        where TEntity : class
    {
        var newValue = createFactory.Invoke(key);

        return set.ToLinqToDBTable()
            .Merge()
            .Using([newValue])
            .OnTargetKey()
            .InsertWhenNotMatched(setterExpression)
            .MergeWithOutputAsync(mergeExpression)
            .FirstAsync();
    }

    private static async Task<TEntity> GetOrCreateAsync<TKey1, TKey2, TEntity>(this Microsoft.EntityFrameworkCore.DbSet<TEntity> set, TKey1 key1,
        TKey2 key2, Func<TKey1, TKey2, TEntity> createFactory, Expression<Func<TEntity, TEntity>> setterExpression, Expression<Func<string, TEntity, TEntity, TEntity, TEntity>> mergeExpression)
        where TEntity : class
    {
        var newValue = createFactory.Invoke(key1, key2);

        var value = await set//.ToLinqToDBTable()
            .Merge()
            .Using([newValue])
            .OnTargetKey()
            .InsertWhenNotMatched(setterExpression)
            .MergeWithOutputAsync(mergeExpression)
            .FirstAsync();

        set.Attach(value);
        return value;
    }
    */

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
}
