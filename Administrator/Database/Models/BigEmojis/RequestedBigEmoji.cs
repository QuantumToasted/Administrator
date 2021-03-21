using System.Threading.Tasks;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class RequestedBigEmoji : BigEmoji,
        IEntityTypeConfiguration<RequestedBigEmoji>
    {
#if !MIGRATION_MODE
        public RequestedBigEmoji(IGuildEmoji emoji, IUser requester)
            : base(emoji.GuildId, emoji.Id, emoji.Name, emoji.IsAnimated)
        {
            RequesterId = requester.Id;
            RequesterTag = requester.Tag;
        }
#endif
        
        public Snowflake RequesterId { get; set; }
        
        public string RequesterTag { get; set; }

        public async Task ApproveAsync(AdminDbContext ctx, IUser approver)
        {
            ctx.BigEmojis.Remove(this);
            await ctx.SaveChangesAsync();
#if !MIGRATION_MODE
            ctx.BigEmojis.Add(new ApprovedBigEmoji(this, approver));
            // Don't save again, it'll be saved later
#endif
        }

        public async Task DenyAsync(AdminDbContext ctx, IUser denier)
        {
            ctx.BigEmojis.Remove(this);
            await ctx.SaveChangesAsync();
#if !MIGRATION_MODE
            ctx.BigEmojis.Add(new DeniedBigEmoji(this, denier));
            // Don't save again, it'll be saved later
#endif
        }

        void IEntityTypeConfiguration<RequestedBigEmoji>.Configure(EntityTypeBuilder<RequestedBigEmoji> builder)
        {
            builder.HasBaseType<BigEmoji>();
        }
    }
}