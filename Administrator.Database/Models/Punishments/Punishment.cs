using System.ComponentModel.DataAnnotations.Schema;
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
    [property: Column("guild")] ulong GuildId, 
    [property: Column("target")] ulong TargetId, 
    [property: Column("target_name")] string TargetName, 
    [property: Column("moderator")] ulong ModeratorId, 
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
    public ulong? LogChannelId { get; set; }
    
    [Column("log_message")]
    public ulong? LogMessageId { get; set; }
    
    [Column("dm_channel")]
    public ulong? DmChannelId { get; set; }
    
    [Column("dm_message")]
    public ulong? DmMessageId { get; set; }
    
    [Column("attachment")]
    public Guid? AttachmentId { get; set; }
    
    [ForeignKey(nameof(AttachmentId))]
    public Attachment? Attachment { get; set; }
    
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