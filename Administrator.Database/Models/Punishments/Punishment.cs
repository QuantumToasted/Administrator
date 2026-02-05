using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public abstract partial record Punishment : IPunishment, IEntityTypeConfiguration<Punishment>
{
    public int Id { get; init; }
    
    public Snowflake GuildId { get; init; }

    public UserSnapshot Target { get; init; } = null!;

    public UserSnapshot Moderator { get; init; } = null!;
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public string? Reason { get; set; }
    
    public abstract PunishmentType Type { get; }
    
    public Guid? AttachmentId { get; init; }
    
    public Snowflake? LogChannelId { get; set; }
    
    public Snowflake? LogMessageId { get; set; }
    
    public Snowflake? DmChannelId { get; set; }
    
    public Snowflake? DmMessageId { get; set; }
    
    public RemoteAttachment? Attachment { get; set; }
    
    public GuildConfiguration? Guild { get; init; }
    
    public sealed override string ToString()
        => this.FormatKey();

    void IEntityTypeConfiguration<Punishment>.Configure(EntityTypeBuilder<Punishment> punishment)
    {
        punishment.HasKey(x => x.Id);
        punishment.HasIndex(x => x.GuildId);

        punishment.Ignore(x => x.Type);

        punishment.Property(x => x.Reason).HasMaxLength(Discord.Limits.Rest.MaxAuditLogReasonLength);
        punishment.Property(x => x.Target).HasColumnType("jsonb");
        punishment.Property(x => x.Moderator).HasColumnType("jsonb");
        punishment.Property(x => x.Reason).HasMaxLength(Discord.Limits.Message.Embed.Field.MaxValueLength);

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