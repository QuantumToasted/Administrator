using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record RoleLevelReward(Snowflake GuildId, int Tier, int Level)
{
    public List<Snowflake> GrantedRoleIds { get; set; } = new();

    public List<Snowflake> RevokedRoleIds { get; set; } = new();
    
    public Guild? Guild { get; init; }

    private sealed class RoleLevelRewardConfiguration : IEntityTypeConfiguration<RoleLevelReward>
    {
        public void Configure(EntityTypeBuilder<RoleLevelReward> reward)
        {
            reward.HasKey(x => new { x.GuildId, x.Tier, x.Level });
        }
    }
}