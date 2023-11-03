using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Kick(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason)
    : Punishment(GuildId, Target, Moderator, Reason), IStaticEntityTypeConfiguration<Kick>
{
    //public override PunishmentType Type { get; init; } = PunishmentType.Kick;
    
    public static void ConfigureBuilder(EntityTypeBuilder<Kick> kick)
    {
        kick.HasBaseType<Punishment>();
    }
}