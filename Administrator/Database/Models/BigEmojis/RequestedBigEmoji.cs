using System;
using System.Threading.Tasks;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class RequestedBigEmoji : BigEmoji,
        IEntityTypeConfiguration<RequestedBigEmoji>
    {
        public Snowflake RequesterId { get; set; }
        
        public string RequesterTag { get; set; }
        
        public DateTimeOffset RequestedAt { get; set; }

        public async Task ApproveAsync(AdminDbContext ctx, IUser approver)
        {
            ctx.BigEmojis.Remove(this);
            await ctx.SaveChangesAsync();
            ctx.BigEmojis.Add(ApprovedBigEmoji.Create(this, approver));
            // Don't save again, it'll be saved later
        }

        public async Task DenyAsync(AdminDbContext ctx, IUser denier)
        {
            ctx.BigEmojis.Remove(this);
            await ctx.SaveChangesAsync();
            ctx.BigEmojis.Add(DeniedBigEmoji.Create(this, denier));
            // Don't save again, it'll be saved later
        }

        public static RequestedBigEmoji Create(IGuildEmoji emoji, IUser requester)
        {
            return new()
            {
                Id = emoji.Id,
                Name = emoji.Name,
                IsAnimated = emoji.IsAnimated,
                GuildId = emoji.GuildId,
                RequesterId = requester.Id,
                RequesterTag = requester.Tag,
                RequestedAt = DateTimeOffset.UtcNow
            };
        }

        void IEntityTypeConfiguration<RequestedBigEmoji>.Configure(EntityTypeBuilder<RequestedBigEmoji> builder)
        {
            builder.HasBaseType<BigEmoji>();
        }
    }
}