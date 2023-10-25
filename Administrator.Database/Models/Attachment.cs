using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("attachments")]
[PrimaryKey(nameof(Id))]
public sealed record Attachment(
    [property: Column("data")] byte[] Data,
    [property: Column("filename")] string FileName)
{
    public Guid Id { get; init; }
}