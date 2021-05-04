using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Highlight : Keyed,
        IEntityTypeConfiguration<Highlight>,
        IUserDbEntity
    {
        public Snowflake UserId { get; set; }

        public string Text { get; set; }

        public Snowflake? GuildId { get; set; }
        
        public static Highlight Create(IUser user, IGuild guild, string text)
        {
            return new()
            {
                UserId = user.Id,
                GuildId = guild?.Id,
                Text = text
            };
        }

        void IEntityTypeConfiguration<Highlight>.Configure(EntityTypeBuilder<Highlight> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();
        }
    }
}