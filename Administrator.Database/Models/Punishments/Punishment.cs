using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public enum PunishmentType
{
    Ban,
    Block,
    Kick,
    TimedRole,
    Timeout,
    Warning
}

public abstract record Punishment(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason) : INumberKeyedDbEntity<int>, IStaticEntityTypeConfiguration<Punishment>
{
    public int Id { get; init; }
    
    //public abstract PunishmentType Type { get; init; }
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public string? Reason { get; set; } = Reason;
    
    public Snowflake? LogChannelId { get; set; }
    
    public Snowflake? LogMessageId { get; set; }
    
    public Snowflake? DmChannelId { get; set; }
    
    public Snowflake? DmMessageId { get; set; }
    
    public Guid? AttachmentId { get; set; }
    
    public Attachment? Attachment { get; set; }
    
    public Guild? Guild { get; init; }
    
    static void IStaticEntityTypeConfiguration<Punishment>.ConfigureBuilder(EntityTypeBuilder<Punishment> punishment)
    {
        punishment.ToTable("punishments");
        punishment.HasKey(x => x.Id);
        punishment.HasIndex(x => x.GuildId);
        // punishment.HasIndex(x => x.Target.Id); can't do jsonb indexes :/

        punishment.HasPropertyWithColumnName(x => x.Id, "id");
        punishment.HasPropertyWithColumnName(x => x.GuildId, "guild");
        punishment.HasPropertyWithColumnName(x => x.Target, "target").HasColumnType("jsonb");
        punishment.HasPropertyWithColumnName(x => x.Moderator, "moderator").HasColumnType("jsonb");
        punishment.HasPropertyWithColumnName(x => x.Reason, "reason");
        punishment.HasPropertyWithColumnName(x => x.LogChannelId, "log_channel");
        punishment.HasPropertyWithColumnName(x => x.LogMessageId, "log_message");
        punishment.HasPropertyWithColumnName(x => x.DmChannelId, "dm_channel");
        punishment.HasPropertyWithColumnName(x => x.DmMessageId, "dm_message");
        punishment.HasPropertyWithColumnName(x => x.AttachmentId, "attachment");

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