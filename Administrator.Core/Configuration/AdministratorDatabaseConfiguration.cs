using System.ComponentModel.DataAnnotations;

namespace Administrator.Core;

public sealed class AdministratorDatabaseConfiguration : IAdministratorConfiguration<AdministratorDatabaseConfiguration>
{
    [Required]
    public string ConnectionString { get; init; } = null!;
}