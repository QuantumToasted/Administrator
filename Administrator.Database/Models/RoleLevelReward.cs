using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("level_rewards")]
[PrimaryKey(nameof(GuildId), nameof(Tier), nameof(Level))]
public sealed record RoleLevelReward(
    [property: Column("guild")] Snowflake GuildId, 
    [property: Column("tier")] int Tier, 
    [property: Column("level")] int Level)
{
    [Column("granted_roles")] 
    public HashSet<Snowflake> GrantedRoleIds { get; init; } = new();

    [Column("revoked_roles")] 
    public HashSet<Snowflake> RevokedRoleIds { get; init; } = new();
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}