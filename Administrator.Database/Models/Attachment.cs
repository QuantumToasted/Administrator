using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Attachment(byte[] Data, string FileName) : IStaticEntityTypeConfiguration<Attachment>
{
    public Guid Id { get; init; }
    
    static void IStaticEntityTypeConfiguration<Attachment>.ConfigureBuilder(EntityTypeBuilder<Attachment> attachment)
    {
        attachment.ToTable("attachments");
        attachment.HasKey(x => x.Id);

        attachment.HasPropertyWithColumnName(x => x.Id, "id");
        attachment.HasPropertyWithColumnName(x => x.Data, "data");
        attachment.HasPropertyWithColumnName(x => x.FileName, "filename");
    }
}