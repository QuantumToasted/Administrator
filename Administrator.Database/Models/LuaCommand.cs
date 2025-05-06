using System.Text;
using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class LuaCommand : ILuaCommand, IEntityTypeConfiguration<LuaCommand>
{
    public Snowflake GuildId { get; init; }

    public string Name { get; init; } = null!;

    public byte[] Metadata { get; init; } = [];

    public byte[] Command { get; init; } = [];

    public byte[] Persistence { get; set; } = [];
    
    public GuildConfiguration? Guild { get; init; }

    public static LuaCommand Create(Snowflake guildId, string name, string metadata, string command, byte[]? persistence = null)
    {
        return new LuaCommand
        {
            GuildId = guildId,
            Name = name,
            Metadata = Compress(metadata),
            Command = Compress(command),
            Persistence = persistence ?? Compress(name)
        };

        static byte[] Compress(string text) => Encoding.Default.GetBytes(text).GZipCompress();
    }

    string ILuaCommand.Metadata => Encoding.Default.GetString(Metadata.GZipDecompress());
    string ILuaCommand.Command => Encoding.Default.GetString(Command.GZipDecompress());
    string ILuaCommand.Persistence => Encoding.Default.GetString(Persistence.GZipDecompress());
    void IEntityTypeConfiguration<LuaCommand>.Configure(EntityTypeBuilder<LuaCommand> command)
    {
        command.HasKey(x => new { x.GuildId, x.Name });
        command.Property(x => x.Name).HasMaxLength(Discord.Limits.ApplicationCommand.MaxNameLength);
    }
}