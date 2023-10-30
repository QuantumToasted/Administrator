using System.ComponentModel.DataAnnotations.Schema;
using Disqord;

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
        Snowflake GuildId, 
        Snowflake TargetId, 
        string TargetName, 
        Snowflake ModeratorId, 
        string ModeratorName, 
        string? Reason)
    : Punishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason)
{
    [Column("revoked")]
    public DateTimeOffset? RevokedAt { get; set; }
    
    [Column("revoker")]
    public Snowflake? RevokerId { get; set; }
    
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
    public Snowflake? AppealChannelId { get; set; }
    
    [Column("appeal_message")]
    public Snowflake? AppealMessageId { get; set; }
}