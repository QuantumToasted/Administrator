using Disqord;
using Humanizer;

namespace Administrator.Bot;

public static class DiscordExtensions
{
    extension<TAttachment>(TAttachment attachment) where TAttachment : IAttachment
    {
        public ByteSize Size => ByteSize.FromBytes(attachment.FileSize);
    }
}