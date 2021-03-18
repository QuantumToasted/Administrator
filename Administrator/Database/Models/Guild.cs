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
        ICached
    {
#if !MIGRATION_MODE
        public Guild(IGuild guild)
        {
            Id = guild.Id;
            Name = guild.Name;
        }
#endif
        
        public Snowflake Id { get; set; }
        
        public string Name { get; set; }
        
        public Snowflake MuteRoleId { get; set; }

        string ICached.CacheKey => $"G:{Id}";
        TimeSpan ICached.SlidingExpiration => TimeSpan.FromMinutes(1);
        void IEntityTypeConfiguration<Guild>.Configure(EntityTypeBuilder<Guild> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
}