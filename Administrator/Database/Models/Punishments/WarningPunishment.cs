using System;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class WarningPunishment : IEntityTypeConfiguration<WarningPunishment>,
        IGuildDbEntity
    {
        public Snowflake GuildId { get; set; }
        
        public int Count { get; set; }
        
        public WarningPunishmentType Type { get; set; }
        
        public TimeSpan? Duration { get; set; }
        
        void IEntityTypeConfiguration<WarningPunishment>.Configure(EntityTypeBuilder<WarningPunishment> builder)
        {
            builder.HasKey(x => new {x.GuildId, x.Count});
        }
    }
}