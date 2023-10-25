using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public enum AppealStatus
{
    Sent,
    NeedsInfo,
    Updated,
    Rejected,
    Ignored
}

public abstract record RevocablePunishment(
        ulong GuildId, 
        ulong TargetId, 
        string TargetName, 
        ulong ModeratorId, 
        string ModeratorName, 
        string? Reason)
    : Punishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason)
{
    [Column("revoked")]
    public DateTimeOffset? RevokedAt { get; set; }
    
    [Column("revoker")]
    public ulong? RevokerId { get; set; }
    
    [Column("revoker_name")]
    public string? RevokerName { get; set; }
    
    [Column("revocation_reason")]
    public string? RevocationReason { get; set; }
    
    [Column("appealed")]
    public DateTimeOffset? AppealedAt { get; set; }
    
    [Column("appeal")]
    public string? AppealText { get; set; }
    
    [Column("appeal_status")]
    public AppealStatus? AppealStatus { get; set; }
    
    [Column("appeal_channel")]
    public ulong? AppealChannelId { get; set; }
    
    [Column("appeal_message")]
    public ulong? AppealMessageId { get; set; }
}