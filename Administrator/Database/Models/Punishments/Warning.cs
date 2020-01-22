using System.IO;
using Disqord;

namespace Administrator.Database
{
    public sealed class Warning : RevocablePunishment
    {
        public Warning(ulong guildId, ulong targetId, ulong moderatorId, string reason, MemoryStream image = null, ImageFormat format = ImageFormat.Default) 
            : base(guildId, targetId, moderatorId, reason, false, image, format)
        { }

        public int? SecondaryPunishmentId{ get; set; }

        public void SetSecondaryPunishment(Punishment punishment)
        {
            SecondaryPunishmentId = punishment.Id;
        }
    }
}