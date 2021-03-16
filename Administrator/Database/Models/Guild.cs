using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Guild : IEntityTypeConfiguration<Guild>, 
        ICached,
        ICleanupHandler<LeftGuildEventArgs>//,
        //IUpdateHandler<GuildUpdatedEventArgs>
    {
        public Snowflake Id { get; set; }
        
        public string Name { get; set; }

        public string CacheKey => $"G:{Id}";
        
        public TimeSpan SlidingExpiration => TimeSpan.FromMinutes(1);
        
        public void Configure(EntityTypeBuilder<Guild> builder)
        {
            builder.HasKey(x => x.Id);
        }

        public Task<List<object>> FindMatches(AdminDbContext ctx, LeftGuildEventArgs e)
            => ctx.Guilds.Where(x => x.Id == e.GuildId).Cast<object>().ToListAsync();
    }
}