using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

public enum InviteFilterExemptionType
{
    Guild = 1, // invites from TargetId (guild)
    Channel, // invites posted in TargetId (channel and channel's threads)
    Role, // invites posted from members with TargetId (role)
    User, // invites posted from TargetId (user)
    InviteCode // invites with the code InviteCode
}

[Table("invite_filter_exemptions")]
[PrimaryKey(nameof(Id))]
[Index(nameof(GuildId))]
public sealed record InviteFilterExemption(
    [property: Column("guild")] ulong GuildId, 
    [property: Column("type")] InviteFilterExemptionType ExemptionType, 
    [property: Column("target")] ulong? TargetId, 
    [property: Column("invite_code")] string? InviteCode)
{
    [Column("id")] 
    public int Id { get; init; }
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}