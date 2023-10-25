using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

public static class DbContextExtensions
{
    public static Task<Guild> GetOrCreateGuildConfigAsync(this AdminDbContext db, ulong guildId)
        => db.GetOrCreateAsync(() => db.Guilds.FindAsync(guildId).AsTask(), () => new Guild(guildId));

    public static Task<GlobalUser> GetOrCreateGlobalUserAsync(this AdminDbContext db, ulong userId)
        => db.GetOrCreateAsync(() => db.GlobalUsers.FindAsync(userId).AsTask(), () => new GlobalUser(userId));
    
    public static Task<GuildUser> GetOrCreateGuildUserAsync(this AdminDbContext db, ulong guildId, ulong memberId)
        => db.GetOrCreateAsync(() => db.GuildUsers.FindAsync(guildId, memberId).AsTask(), () => new GuildUser(memberId, guildId));

    public static Task<EmojiStats> GetOrCreateEmojiStatisticsAsync(this AdminDbContext db, ulong guildId, ulong emojiId)
        => db.GetOrCreateAsync(() => db.EmojiStats.FindAsync(emojiId).AsTask(), () => new EmojiStats(emojiId, guildId));
    
    private static async Task<T> GetOrCreateAsync<T>(this DbContext db, Func<Task<T?>> getFactory, Func<T> createFactory)
    {
        var dbValue = await getFactory();
        if (dbValue is not null)
            return dbValue;

        var newValue = createFactory();
        db.Add(newValue!);
        await db.SaveChangesAsync();
        return newValue;
    }
    
    /*
    public static ValueTask<GuildConfiguration> GetOrCreateGuildConfigAsync(this AdminDbContext db, ulong guildId)
    {
        return db.GetOrFetchOrCreateAsync(
            ICachedDbEntity<GuildConfiguration>.GetCacheKey(guildId),
            () => db.Guilds.FindAsync(guildId).AsTask(),
            () => new GuildConfiguration(guildId));
    }

    public static ValueTask<List<Punishment>> GetPunishmentsAsync(this AdminDbContext db, ulong guildId)
    {
        return db.GetOrFetchAsync(
            IBulkCachedDbEntity<Punishment>.GetBulkCacheKey(guildId),
            () => db.Punishments.Where(x => x.GuildId == guildId).ToListAsync()!)!;
    }

    public static ValueTask<LoggingChannel?> GetLoggingChannelAsync(this AdminDbContext db, ulong guildId, LogEventType eventType)
    {
        return db.GetOrFetchAsync(
            ICachedDbEntity<LoggingChannel>.GetCacheKey(guildId, eventType),
            () => db.LoggingChannels.FindAsync(guildId, eventType).AsTask());
    }

    public static ValueTask<List<Highlight>> GetHighlightsAsync(this AdminDbContext db)
    {
        return db.GetOrFetchAsync(
            IBulkCachedDbEntity<Highlight>.GetBulkCacheKey(),
            () => db.Highlights.ToListAsync()!)!;
    }

    public static ValueTask<GlobalUser> GetOrCreateGlobalUserAsync(this AdminDbContext db, ulong userId)
    {
        return db.GetOrFetchOrCreateAsync(
                ICachedDbEntity<GlobalUser>.GetCacheKey(userId),
                () => db.GlobalUsers.FindAsync(userId).AsTask(),
                () => new GlobalUser(userId));
    }

    public static ValueTask<GuildUser> GetOrCreateGuildUserAsync(this AdminDbContext db, ulong guildId, ulong memberId)
    {
        return db.GetOrFetchOrCreateAsync(
            ICachedDbEntity<GuildUser>.GetCacheKey(guildId, memberId),
            () => db.GuildUsers.FindAsync(guildId, memberId).AsTask(),
            () => new GuildUser(guildId, memberId));
    }

    public static ValueTask<EmojiStatistics> GetOrCreateEmojiStatisticsAsync(this AdminDbContext db, ulong emojiId)
    {
        return db.GetOrFetchOrCreateAsync(
            ICachedDbEntity<EmojiStatistics>.GetCacheKey(emojiId),
            () => db.EmojiStats.FindAsync(emojiId).AsTask(),
            () => new EmojiStatistics(emojiId));
    }

    public static ValueTask<List<ForumAutoTag>> GetAutoTags(this AdminDbContext db, ulong channelId)
    {
        return db.GetOrFetchAsync(
            IBulkCachedDbEntity<ForumAutoTag>.GetBulkCacheKey(channelId),
            () => db.AutoTags.Where(x => x.ChannelId == channelId).ToListAsync()!)!;
    }
    */
}
