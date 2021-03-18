﻿using System.Threading;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
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

        public async ValueTask<Guild> GetOrCreateGuildAsync(IGuild guild)
#if !MIGRATION_MODE
        {
            if (_cache.TryGetValue($"G:{guild.Id}", out Guild cacheGuild))
                return cacheGuild;

            if (await FindAsync<Guild>(guild.Id) is { } dbGuild)
                return dbGuild;

            var entry = Add(new Guild(guild));
            await SaveChangesAsync();
            return entry.Entity;
        }
#else
            => default;
#endif

        public override async ValueTask<TEntity> FindAsync<TEntity>(params object[] keyValues)
        {
            var entity = await base.FindAsync<TEntity>(keyValues);

            if (entity is ICached cached)
            {
                _logger.LogInformation("Hi! Caching entity with key {CacheKey} now.", cached.CacheKey);
                _cache.Set(cached.CacheKey, entity,
                    new MemoryCacheEntryOptions().SetSlidingExpiration(cached.SlidingExpiration));
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
                            new MemoryCacheEntryOptions().SetSlidingExpiration(entry.Entity.SlidingExpiration));
                        break;
                }
            }
            
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(Guild).Assembly);
            builder.DefineGlobalConversion<Snowflake, ulong>(x => x.RawValue, x => new Snowflake(x));
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