using System.Text;
using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record LuaCommand(Snowflake GuildId, string Name, byte[] Metadata, byte[] Command)
{
    public byte[] Metadata { get; set; } = Metadata;

    public byte[] Command { get; set; } = Command;

    public byte[] Persistence { get; set; } = Encoding.Default.GetBytes(Name).GZipCompress();
    
    public Guild? Guild { get; init; }

    private sealed class LuaCommandConfiguration : IEntityTypeConfiguration<LuaCommand>
    {
        public void Configure(EntityTypeBuilder<LuaCommand> command)
        {
            command.HasKey(x => new { x.GuildId, x.Name });
        }
    }
}