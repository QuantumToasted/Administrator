using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public abstract record RevocablePunishment : Punishment, IRevocablePunishment, IEntityTypeConfiguration<RevocablePunishment>
{
    public DateTimeOffset? RevokedAt { get; set; }
    
    public UserSnapshot? Revoker { get; set; }
    
    public string? RevocationReason { get; set; }
    
    public DateTimeOffset? AppealedAt { get; set; }
    
    public string? AppealText { get; set; }
    
    public AppealStatus? AppealStatus { get; set; }
    
    public Snowflake? AppealChannelId { get; set; }
    
    public Snowflake? AppealMessageId { get; set; }

    void IEntityTypeConfiguration<RevocablePunishment>.Configure(EntityTypeBuilder<RevocablePunishment> punishment)
    {
        punishment.HasBaseType<Punishment>();
        
        punishment.Property(x => x.Revoker).HasColumnType("jsonb");
        punishment.Property(x => x.RevocationReason).HasMaxLength(Discord.Limits.Message.Embed.Field.MaxValueLength);
        punishment.Property(x => x.AppealText).HasMaxLength(Discord.Limits.Message.Embed.Field.MaxValueLength);
    }
}