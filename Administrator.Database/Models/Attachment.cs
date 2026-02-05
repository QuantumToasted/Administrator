using Administrator.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record RemoteAttachment : IRemoteAttachment, IEntityTypeConfiguration<RemoteAttachment>
{
    public Guid Key { get; init; } = Guid.NewGuid();

    public string FileName { get; init; } = null!;

    public static RemoteAttachment Create(string fileName)
    {
        return new RemoteAttachment
        {
            FileName = fileName
        };
    }
    
    public void Configure(EntityTypeBuilder<RemoteAttachment> attachment)
    {
        attachment.HasKey(x => x.Key);
        attachment.Property(x => x.FileName).HasMaxLength(200);
    }
}