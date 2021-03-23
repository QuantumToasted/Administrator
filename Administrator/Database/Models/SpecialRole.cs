using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class SpecialRole : ICached, IEntityTypeConfiguration<SpecialRole>
    {
        public Snowflake GuildId { get; set; }
        
        public SpecialRoleType Type { get; set; }

        public Snowflake RoleId { get; set; }

        public static SpecialRole Create(IGuild guild, IRole role, SpecialRoleType type)
        {
            return new()
            {
                GuildId = guild.Id,
                RoleId = role.Id,
                Type = type
            };
        }

        string ICached.CacheKey => $"SR:{GuildId}:{Type:D}";
        void IEntityTypeConfiguration<SpecialRole>.Configure(EntityTypeBuilder<SpecialRole> builder)
        {
            builder.HasKey(x => new {x.GuildId, x.Type});
        }
    }
}