using System.Collections.Generic;
using Disqord;
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
        
        public List<string> Prefixes { get; set; }

        public bool AddPrefix(string prefix)
        {
            if (Prefixes.Contains(prefix))
                return false;
            
            Prefixes.Add(prefix);
            return true;
        }

        string ICached.CacheKey => $"G:{Id}";
        void IEntityTypeConfiguration<Guild>.Configure(EntityTypeBuilder<Guild> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
}