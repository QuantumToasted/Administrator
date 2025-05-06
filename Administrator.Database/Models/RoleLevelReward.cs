using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record RoleLevelReward : IRoleLevelReward, IEntityTypeConfiguration<RoleLevelReward>
{
    public Snowflake GuildId { get; init; }
    
    public int Tier { get; init; }
    
    public int Level { get; init; }

    public List<Snowflake> GrantedRoleIds { get; set; } = [];

    public List<Snowflake> RevokedRoleIds { get; set; } = [];
    
    public GuildConfiguration? Guild { get; init; }

    public static RoleLevelReward Create(Snowflake guildId, int tier, int level, IEnumerable<Snowflake> grantedRoleIds, IEnumerable<Snowflake> revokedRoleIds)
    {
        return new RoleLevelReward
        {
            GuildId = guildId,
            Tier = tier,
            Level = level,
            GrantedRoleIds = grantedRoleIds.ToList(),
            RevokedRoleIds = revokedRoleIds.ToList()
        };
    }
    
    IReadOnlyList<Snowflake> IRoleLevelReward.GrantedRoleIds => GrantedRoleIds;
    IReadOnlyList<Snowflake> IRoleLevelReward.RevokedRoleIds => RevokedRoleIds;
    void IEntityTypeConfiguration<RoleLevelReward>.Configure(EntityTypeBuilder<RoleLevelReward> reward)
    {
        reward.HasKey(x => new { x.GuildId, x.Tier, x.Level });
    }
}