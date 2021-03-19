using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class SpecialEmoji : ICached, IEntityTypeConfiguration<SpecialEmoji>
    {
#if !MIGRATION_MODE
        public SpecialEmoji(IGuild guild, IEmoji emoji, SpecialEmojiType type)
        {
            GuildId = guild.Id;
            Emoji = emoji;
            Type = type;
        }
#endif
        
        public Snowflake GuildId { get; set; }

        public SpecialEmojiType Type { get; set; }
        
        public IEmoji Emoji { get; set; }
        
        string ICached.CacheKey => $"SE:{GuildId}:{Type:D}";
        void IEntityTypeConfiguration<SpecialEmoji>.Configure(EntityTypeBuilder<SpecialEmoji> builder)
        {
            builder.HasKey(x => new {x.GuildId, x.Type});
        }
    }
}