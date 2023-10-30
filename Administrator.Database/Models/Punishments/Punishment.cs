using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;

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

[Table("punishments")]
[PrimaryKey(nameof(Id))]
[Index(nameof(GuildId))]
[Index(nameof(TargetId))]
public abstract record Punishment(
    [property: Column("guild")] Snowflake GuildId, 
    [property: Column("target")] Snowflake TargetId, 
    [property: Column("target_name")] string TargetName, 
    [property: Column("moderator")] Snowflake ModeratorId, 
    [property: Column("moderator_name")] string ModeratorName,
    string? Reason) : INumberKeyedDbEntity<int>
{
    [Column("id")]
    public int Id { get; init; }
    
    [Column("type")]
    public abstract PunishmentType Type { get; init; }
    
    [Column("created")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("reason")]
    public string? Reason { get; set; } = Reason;
    
    [Column("log_channel")]
    public Snowflake? LogChannelId { get; set; }
    
    [Column("log_message")]
    public Snowflake? LogMessageId { get; set; }
    
    [Column("dm_channel")]
    public Snowflake? DmChannelId { get; set; }
    
    [Column("dm_message")]
    public Snowflake? DmMessageId { get; set; }
    
    [Column("attachment")]
    public Guid? AttachmentId { get; set; }
    
    [ForeignKey(nameof(AttachmentId))]
    public Attachment? Attachment { get; set; }
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
    
    /*
    void IEntityTypeConfiguration<Punishment>.Configure(EntityTypeBuilder<Punishment> punishment)
    {
        punishment.HasKey(x => x.Id);
        punishment.HasDiscriminator<string>("type")
            .HasValue<Ban>("ban")
            .HasValue<Block>("block")
            .HasValue<Kick>("kick")
            .HasValue<Timeout>("timeout")
            .HasValue<Warning>("warning")
            .HasValue<TimedRole>("timed_role");
        
        // TODO: https://github.com/efcore/EFCore.NamingConventions/issues/184
        // remove when fixed
        punishment.ToTable("punishments");
    }
    */
}