using Administrator.Core;
using Disqord;
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

public abstract record RevocablePunishment(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason)
    : Punishment(GuildId, Target, Moderator, Reason), IStaticEntityTypeConfiguration<RevocablePunishment>
{
    public DateTimeOffset? RevokedAt { get; set; }
    
    public UserSnapshot? Revoker { get; set; }
    
    public string? RevocationReason { get; set; }
    
    public DateTimeOffset? AppealedAt { get; set; }
    
    public string? AppealText { get; set; }
    
    public AppealStatus? AppealStatus { get; set; }
    
    public Snowflake? AppealChannelId { get; set; }
    
    public Snowflake? AppealMessageId { get; set; }

    static void IStaticEntityTypeConfiguration<RevocablePunishment>.ConfigureBuilder(EntityTypeBuilder<RevocablePunishment> punishment)
    {
        punishment.HasBaseType<Punishment>();
        
        punishment.HasPropertyWithColumnName(x => x.RevokedAt, "revoked");
        punishment.HasPropertyWithColumnName(x => x.Revoker, "revoker").HasColumnType("jsonb");
        punishment.HasPropertyWithColumnName(x => x.RevocationReason, "revocation_reason");
        punishment.HasPropertyWithColumnName(x => x.AppealedAt, "appealed");
        punishment.HasPropertyWithColumnName(x => x.AppealText, "appeal");
        punishment.HasPropertyWithColumnName(x => x.AppealStatus, "appeal_status");
        punishment.HasPropertyWithColumnName(x => x.AppealChannelId, "appeal_channel");
        punishment.HasPropertyWithColumnName(x => x.AppealMessageId, "appeal_message");
    }
}