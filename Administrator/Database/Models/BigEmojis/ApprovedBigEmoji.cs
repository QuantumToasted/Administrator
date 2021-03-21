using System;
using Disqord;
using Disqord.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class ApprovedBigEmoji : BigEmoji,
        IEntityTypeConfiguration<ApprovedBigEmoji>,
        ICustomEmoji
    {
#if !MIGRATION_MODE
        public ApprovedBigEmoji(RequestedBigEmoji emoji, IUser approver)
            : base(emoji.GuildId, emoji.Id, emoji.Name, emoji.IsAnimated)
        {
            ApproverId = approver.Id;
            ApproverTag = approver.Tag;
            ApprovedAt = DateTimeOffset.UtcNow;
        }
#endif

        public Snowflake ApproverId { get; set; }

        public string ApproverTag { get; set; }

        public DateTimeOffset ApprovedAt { get; set; }

        void IEntityTypeConfiguration<ApprovedBigEmoji>.Configure(EntityTypeBuilder<ApprovedBigEmoji> builder)
        {
            builder.HasBaseType<BigEmoji>();
        }

        IClient IEntity.Client => throw new NotImplementedException();
        bool IEquatable<IEmoji>.Equals(IEmoji other) => (other as ICustomEmoji)?.Equals(this) == true;
        void IJsonUpdatable<EmojiJsonModel>.Update(EmojiJsonModel model) => throw new NotImplementedException();
        DateTimeOffset ISnowflakeEntity.CreatedAt => Id.CreatedAt;
        string ITaggable.Tag => this.GetMessageFormat();
        bool IEquatable<ICustomEmoji>.Equals(ICustomEmoji other) => other?.Id == Id && other.Name == Name;
    }
}