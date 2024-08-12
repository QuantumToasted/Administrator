using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Attachment(string FileName)
{
    public Guid Key { get; init; } = Guid.NewGuid();

    private sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> attachment)
        {
            attachment.HasKey(x => x.Key);
        }
    }
}