using System.Collections.Generic;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Guild : IEntityTypeConfiguration<Guild>, 
        ICached
    {
        public Snowflake Id { get; set; }
        
        public string Name { get; set; }
        
        public List<string> Prefixes { get; set; }
        
        public int BigEmojiSizeMultiplier { get; set; }
        
        public GuildSetting Settings { get; set; }
        
        public int? BanPruneDays { get; set; }
        
        public List<string> BlacklistedEmojiNames { get; set; }

        public bool AddPrefix(string prefix)
        {
            if (Prefixes.Contains(prefix))
                return false;
            
            Prefixes.Add(prefix);
            return true;
        }

        public static Guild Create(IGuild guild)
        {
            return new()
            {
                Id = guild.Id,
                Name = guild.Name,
                BigEmojiSizeMultiplier = 100,
                Settings = GuildSetting.Punishments
            };
        }

        string ICached.CacheKey => $"G:{Id}";
        void IEntityTypeConfiguration<Guild>.Configure(EntityTypeBuilder<Guild> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Prefixes)
                .HasDefaultValueSql("'{}'");
            builder.Property(x => x.BlacklistedEmojiNames)
                .HasDefaultValueSql("'{}'");
        }
    }
}