﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Disqord.Gateway;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Administrator.Database
{
    public sealed class AdminDbContext : DbContext
    {
        private readonly ILogger<AdminDbContext> _logger;
        private readonly IMemoryCache _cache;
        private readonly EmojiService _emojiService;
        
        public AdminDbContext(ILogger<AdminDbContext> logger, IMemoryCache cache, 
            EmojiService emojiService, DbContextOptions options)
            : base(options)
        {
            _logger = logger;
            _cache = cache;
            _emojiService = emojiService;
        }
        
        public DbSet<Guild> Guilds { get; set; }
        
        public DbSet<Punishment> Punishments { get; set; }
        
        public DbSet<SpecialRole> SpecialRoles { get; set; }
        
        public DbSet<SpecialEmoji> SpecialEmojis { get; set; }
        
        public DbSet<BigEmoji> BigEmojis { get; set; }
        
        public DbSet<WarningPunishment> WarningPunishments { get; set; }
        
        public DbSet<Highlight> Highlights { get; set; }

        public async ValueTask<Guild> GetOrCreateGuildAsync(IGuild guild)
        {
            if (_cache.TryGetValue($"G:{guild.Id}", out Guild cacheGuild))
                return cacheGuild;

            if (await FindAsync<Guild>(guild.Id) is { } dbGuild)
                return dbGuild;

            var entry = Add(Guild.Create(guild));
            await SaveChangesAsync();
            return entry.Entity;
        }

        public async ValueTask<List<Punishment>> GetAllPunishmentsAsync(Snowflake guildId)
        {
            if (_cache.TryGetValue($"P:{guildId}", out List<Punishment> punishments))
                return punishments;

            return _cache.Set($"P:{guildId}", await Punishments.Where(x => x.GuildId == guildId).ToListAsync(),
                TimeSpan.FromMinutes(10));
        }
        
        public async ValueTask<List<TPunishment>> GetPunishmentsAsync<TPunishment>(Snowflake guildId, Func<TPunishment, bool> predicate = null)
        {
            var allPunishments = await GetAllPunishmentsAsync(guildId);
            return (predicate is not null
                ? allPunishments.OfType<TPunishment>().Where(predicate)
                : allPunishments.OfType<TPunishment>()).ToList();
        }

        public async ValueTask<List<BigEmoji>> GetAllBigEmojisAsync()
        {
            if (_cache.TryGetValue("BigEmojis", out List<BigEmoji> emojis))
                return emojis;

            return _cache.Set("BigEmojis", await BigEmojis.ToListAsync(),
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
        }

        public async ValueTask<LoggingChannel> GetLoggingChannelAsync(Snowflake guildId, LoggingChannelType type)
        {
            if (_cache.TryGetValue($"LC:{guildId}:{type:D}", out LoggingChannel cacheChannel))
                return cacheChannel;

            if (await FindAsync<LoggingChannel>(guildId, type) is { } dbChannel)
                return dbChannel;

            return null;
        }
        
        public async ValueTask<SpecialEmoji> GetSpecialEmojiAsync(Snowflake guildId, SpecialEmojiType type)
        {
            if (_cache.TryGetValue($"SE:{guildId}:{type:D}", out SpecialEmoji cacheEmoji))
                return cacheEmoji;

            if (await FindAsync<SpecialEmoji>(guildId, type) is { } dbEmoji)
                return dbEmoji;

            return null;
        }
        
        public async ValueTask<SpecialRole> GetSpecialRoleAsync(Snowflake guildId, SpecialRoleType type)
        {
            if (_cache.TryGetValue($"SR:{guildId}:{type:D}", out SpecialRole cacheRole))
                return cacheRole;

            if (await FindAsync<SpecialRole>(guildId, type) is { } dbRole)
                return dbRole;

            return null;
        }

        public async Task<List<Highlight>> GetHighlightsAsync()
        {
            if (_cache.TryGetValue("Highlights", out List<Highlight> cacheHighlights))
                return cacheHighlights;
            
            return _cache.Set(Highlights, await Highlights.ToListAsync(),
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
        }

        public override async ValueTask<TEntity> FindAsync<TEntity>(params object[] keyValues)
        {
            var entity = await base.FindAsync<TEntity>(keyValues);

            if (entity is ICached cached)
            {
                _cache.Set(cached.CacheKey, entity,
                    new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
            }

            return entity;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<ICached>())
            {
                switch (entry.State)
                {
                    case EntityState.Detached:
                        break;
                    case EntityState.Deleted:
                        _cache.Remove(entry.Entity.CacheKey);
                        break;
                    default:
                        _cache.Set(entry.Entity.CacheKey, entry.Entity,
                            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                        break;
                }
            }

            foreach (var entry in ChangeTracker.Entries<Punishment>().DistinctBy(x => x.Entity.GuildId))
            {
                _cache.Remove($"P:{entry.Entity.GuildId}");
            }

            if (ChangeTracker.Entries<BigEmoji>().Any())
            {
                _cache.Remove("BigEmojis");
            }
            
            if (ChangeTracker.Entries<Highlight>().Any())
            {
                _cache.Remove("Highlights");
            }
            
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(Guild).Assembly);
            builder.DefineGlobalConversion<Snowflake, ulong>(x => x, x => x);
            builder.DefineGlobalConversion<Snowflake?, ulong?>(x => x, x => x);
            builder.DefineGlobalConversion<Upload, string>(x => x.ToString(), x => Upload.Parse(x));
            builder.DefineGlobalConversion<IEmoji, string>(x => x.ToString(), x => _emojiService.ParseEmoji(x));

            foreach (var entity in builder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName().Underscore().ToLower());

                foreach (var property in entity.GetProperties())
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    property.SetColumnName(property
                        .GetColumnName()
#pragma warning restore CS0618 // Type or member is obsolete
                        .Underscore().ToLower());
                }

                foreach (var key in entity.GetKeys())
                {
                    key.SetName(key.GetName().Underscore().ToLower());
                }

                // TODO: Foreign keys? indexes?
            }
        }
    }
}