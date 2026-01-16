using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public class PermissionsPlusModel : IPermissionsPlus, IEntityTypeConfiguration<PermissionsPlusModel>
{
    public ulong GuildId { get; init; }
    
    public ulong TargetId { get; init; }
    
    public PermissionsTargetType TargetType { get; init; }
    
    public PermissionsPlus Permissions { get; init; }
    
    Snowflake IPermissionsPlus.GuildId => GuildId;
    Snowflake IPermissionsPlus.TargetId => TargetId;
    public void Configure(EntityTypeBuilder<PermissionsPlusModel> builder)
    {
        throw new NotImplementedException();
    }
}