using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record RoleLevelReward(Snowflake GuildId, int Tier, int Level) : IStaticEntityTypeConfiguration<RoleLevelReward>
{
    public HashSet<Snowflake> GrantedRoleIds { get; set; } = new();

    public HashSet<Snowflake> RevokedRoleIds { get; set; } = new();
    
    public Guild? Guild { get; init; }

    static void IStaticEntityTypeConfiguration<RoleLevelReward>.ConfigureBuilder(EntityTypeBuilder<RoleLevelReward> reward)
    {
        reward.ToTable("level_rewards");
        reward.HasKey(x => new { x.GuildId, x.Tier, x.Level });

        reward.HasPropertyWithColumnName(x => x.GuildId, "guild");
        reward.HasPropertyWithColumnName(x => x.Tier, "tier");
        reward.HasPropertyWithColumnName(x => x.Level, "level");
        reward.HasPropertyWithColumnName(x => x.GrantedRoleIds, "granted_roles");
        reward.HasPropertyWithColumnName(x => x.RevokedRoleIds, "revoked_roles");
    }
}