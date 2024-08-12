using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Kick(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason)
    : Punishment(GuildId, Target, Moderator, Reason), IKick
{
    public override PunishmentType Type => PunishmentType.Kick;

    private sealed class KickConfiguration : IEntityTypeConfiguration<Kick>
    {
        public void Configure(EntityTypeBuilder<Kick> kick)
        {
            kick.HasBaseType<Punishment>();
        }
    }
}