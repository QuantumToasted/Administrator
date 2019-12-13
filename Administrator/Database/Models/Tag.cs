using System;
using System.IO;
using Disqord;

namespace Administrator.Database
{
    public sealed class Tag
    {
        private Tag()
        { }

        public Tag(ulong guildId, ulong authorId, string name, string response, MemoryStream image, ImageFormat format)
        {
            GuildId = guildId;
            AuthorId = authorId;
            Name = name;
            Response = response;
            CreatedAt = DateTimeOffset.UtcNow;
            Image = image;
            Format = format;
        }

        public ulong GuildId { get; set; }

        public ulong AuthorId { get; set; }

        public string Name { get; set; }

        public string Response { get; set; }

        public MemoryStream Image { get; set; } = new MemoryStream();

        public ImageFormat Format { get; set; } = ImageFormat.Default;

        public DateTimeOffset CreatedAt { get; set; }

        public int Uses { get; set; }
    }
}