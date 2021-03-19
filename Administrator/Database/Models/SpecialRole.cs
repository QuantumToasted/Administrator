using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class SpecialRole : ICached, IEntityTypeConfiguration<SpecialRole>
    {
#if !MIGRATION_MODE
        public SpecialRole(IGuild guild, IRole role, SpecialRoleType type)
        {
            GuildId = guild.Id;
            RoleId = role.Id;
            Type = type;
        }
#endif
        
        public Snowflake GuildId { get; set; }
        
        public SpecialRoleType Type { get; set; }

        public Snowflake RoleId { get; set; }

        string ICached.CacheKey => $"SR:{GuildId}:{Type:D}";
        void IEntityTypeConfiguration<SpecialRole>.Configure(EntityTypeBuilder<SpecialRole> builder)
        {
            builder.HasKey(x => new {x.GuildId, x.Type});
        }
    }
}