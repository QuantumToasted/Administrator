using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class LoggingChannel : IEntityTypeConfiguration<LoggingChannel>,
        ICached,
        IGuildDbEntity
    {
        public Snowflake Id { get; set; }
        
        public Snowflake GuildId { get; set; }
        
        public LoggingChannelType Type { get; set; }
        
        void IEntityTypeConfiguration<LoggingChannel>.Configure(EntityTypeBuilder<LoggingChannel> builder)
        {
            builder.HasKey(x => new {x.GuildId, x.Type});
        }

        string ICached.CacheKey => $"LC:{GuildId}:{Type:D}";
    }
}