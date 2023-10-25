using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("lua_commands")]
[PrimaryKey(nameof(GuildId), nameof(Name))]
public sealed record LuaCommand(
    [property: Column("guild")] ulong GuildId, 
    [property: Column("name")] string Name,
    byte[] Metadata,
    byte[] Command)
{
    [property: Column("metadata")]
    public byte[] Metadata { get; set; } = Metadata;

    [property: Column("command")]
    public byte[] Command { get; set; } = Command;
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}