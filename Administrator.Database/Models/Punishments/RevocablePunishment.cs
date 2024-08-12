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
    : Punishment(GuildId, Target, Moderator, Reason), IRevocablePunishment
{
    public DateTimeOffset? RevokedAt { get; set; }
    
    public UserSnapshot? Revoker { get; set; }
    
    public string? RevocationReason { get; set; }
    
    public DateTimeOffset? AppealedAt { get; set; }
    
    public string? AppealText { get; set; }
    
    public AppealStatus? AppealStatus { get; set; }
    
    public Snowflake? AppealChannelId { get; set; }
    
    public Snowflake? AppealMessageId { get; set; }

    private sealed class RevocablePunishmentConfiguration : IEntityTypeConfiguration<RevocablePunishment>
    {
        public void Configure(EntityTypeBuilder<RevocablePunishment> punishment)
        {
            punishment.HasBaseType<Punishment>();
        
            punishment.Property(x => x.Revoker).HasColumnType("jsonb");
        }
    }
}