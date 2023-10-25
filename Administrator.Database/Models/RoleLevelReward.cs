using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("level_rewards")]
[PrimaryKey(nameof(GuildId), nameof(Tier), nameof(Level))]
public sealed record RoleLevelReward(
    [property: Column("guild")] ulong GuildId, 
    [property: Column("tier")] int Tier, 
    [property: Column("level")] int Level)
{
    [Column("granted_roles")] 
    public HashSet<ulong> GrantedRoleIds { get; init; } = new();

    [Column("revoked_roles")] 
    public HashSet<ulong> RevokedRoleIds { get; init; } = new();
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}