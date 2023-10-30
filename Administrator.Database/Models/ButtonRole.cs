using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

// TODO: implement button roles
[Table("button_roles")]
[PrimaryKey(nameof(Id))]
public sealed record ButtonRole(
    [property: Column("guild")] Snowflake GuildId,
    [property: Column("channel")] Snowflake ChannelId,
    [property: Column("message")] Snowflake MessageId,
    [property: Column("row")] int Row,
    [property: Column("position")] int Position,
    [property: Column("emoji")] string? Emoji,
    [property: Column("text")] string? Text,
    [property: Column("role")] Snowflake RoleId)
{
    [Column("id")]
    public int Id { get; init; }
    
    [Column("exclusive_group")]
    public int? ExclusiveGroupId { get; set; }
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}