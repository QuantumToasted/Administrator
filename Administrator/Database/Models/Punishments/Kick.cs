using System.IO;
using Disqord;

namespace Administrator.Database
{
    public sealed class Kick : Punishment
    {
        public Kick(ulong guildId, ulong targetId, ulong moderatorId, string reason, MemoryStream image = null, ImageFormat format = ImageFormat.Default) 
            : base(guildId, targetId, moderatorId, reason, image, format)
        { }
    }
}