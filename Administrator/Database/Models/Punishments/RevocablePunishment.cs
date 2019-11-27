using System;
using System.IO;
using Disqord;

namespace Administrator.Database
{
    public abstract class RevocablePunishment : Punishment
    {
        protected RevocablePunishment(ulong guildId, ulong targetId, ulong moderatorId, string reason, bool isAppealable, MemoryStream image = null, ImageFormat format = ImageFormat.Default) 
            : base(guildId, targetId, moderatorId, reason, image, format)
        {
            IsAppealable = isAppealable;
        }

        public bool IsAppealable { get; set; }

        public DateTimeOffset? RevokedAt { get; set; }

        public ulong RevokerId { get; set; }

        public string RevocationReason { get; set; }

        public DateTimeOffset? AppealedAt { get; set; }

        public string AppealReason { get; set; }

        public void Revoke(ulong revokerId, string reason)
        {
            RevokedAt = DateTimeOffset.UtcNow;
            RevokerId = revokerId;
            RevocationReason = reason;
        }

        public void Appeal(string reason)
        {
            AppealedAt = DateTimeOffset.UtcNow;
            AppealReason = reason;
        }
    }
}