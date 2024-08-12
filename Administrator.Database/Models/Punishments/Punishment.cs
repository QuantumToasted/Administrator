using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public abstract record Punishment(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason) : INumberKeyedDbEntity<int>, IPunishment
{
    public int Id { get; init; }
    
    public abstract PunishmentType Type { get; }
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public string? Reason { get; set; } = Reason;

    public Snowflake? LogChannelId { get; set; }
    
    public Snowflake? LogMessageId { get; set; }
    
    public Snowflake? DmChannelId { get; set; }
    
    public Snowflake? DmMessageId { get; set; }
    
    public Guid? AttachmentId { get; set; }
    
    public Attachment? Attachment { get; set; }
    
    public Guild? Guild { get; init; }

    public sealed override string ToString()
        => this.FormatKey();

    private sealed class PunishmentConfiguration : IEntityTypeConfiguration<Punishment>
    {
        public void Configure(EntityTypeBuilder<Punishment> punishment)
        {
            punishment.HasKey(x => x.Id);
            punishment.HasIndex(x => x.GuildId);

            punishment.Ignore(x => x.Type);
            
            punishment.Property(x => x.Target).HasColumnType("jsonb");
            punishment.Property(x => x.Moderator).HasColumnType("jsonb");

            punishment.HasOne(x => x.Attachment);
            punishment.HasDiscriminator<PunishmentType>("type")
                .HasValue<Ban>(PunishmentType.Ban)
                .HasValue<Block>(PunishmentType.Block)
                .HasValue<Kick>(PunishmentType.Kick)
                .HasValue<TimedRole>(PunishmentType.TimedRole)
                .HasValue<Timeout>(PunishmentType.Timeout)
                .HasValue<Warning>(PunishmentType.Warning)
                .IsComplete();
        }
    }
}