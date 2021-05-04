using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public abstract class RevocablePunishment : Punishment, IEntityTypeConfiguration<RevocablePunishment>
    {
        public DateTimeOffset? RevokedAt { get; set; }

        public Snowflake RevokerId { get; set; }

        public string RevokerTag { get; set; }

        public string RevocationReason { get; set; }

        public DateTimeOffset? AppealedAt { get; set; }

        public string AppealReason { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

        public bool IsRevoked => RevokedAt.HasValue;

        public abstract Task<LocalMessage> FormatRevokedMessageAsync(DiscordBotBase bot);

        public abstract Task<LocalMessage> FormatRevokedDmMessageAsync(DiscordBotBase bot);

        public abstract Task<LocalMessage> FormatAppealMessageAsync(DiscordBotBase bot);

        public void Revoke(IUser revoker, string reason)
        {
            RevokedAt = DateTimeOffset.UtcNow;
            RevokerId = revoker.Id;
            RevokerTag = revoker.Tag;
            RevocationReason = reason;
        }
        
        public void Appeal(string reason)
        {
            AppealedAt = DateTimeOffset.UtcNow;
            AppealReason = reason;
        }
        
        void IEntityTypeConfiguration<RevocablePunishment>.Configure(EntityTypeBuilder<RevocablePunishment> builder)
        {
            builder.HasBaseType<Punishment>();
        }
    }
}