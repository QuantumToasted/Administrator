using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class SpecialEmoji : ICached, IEntityTypeConfiguration<SpecialEmoji>
    {
        public Snowflake GuildId { get; set; }

        public SpecialEmojiType Type { get; set; }
        
        public IEmoji Emoji { get; set; }

        public static SpecialEmoji Create(IGuild guild, IEmoji emoji, SpecialEmojiType type)
        {
            return new()
            {
                GuildId = guild.Id,
                Emoji = emoji,
                Type = type
            };
        }
        
        string ICached.CacheKey => $"SE:{GuildId}:{Type:D}";
        void IEntityTypeConfiguration<SpecialEmoji>.Configure(EntityTypeBuilder<SpecialEmoji> builder)
        {
            builder.HasKey(x => new {x.GuildId, x.Type});
        }
    }
}