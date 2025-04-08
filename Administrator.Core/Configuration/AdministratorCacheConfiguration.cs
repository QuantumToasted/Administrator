using System.ComponentModel.DataAnnotations;

namespace Administrator.Core;

public sealed class AdministratorCacheConfiguration : IAdministratorConfiguration<AdministratorCacheConfiguration>
{
    [Required]
    public int MaxMessageLifetimeDays { get; init; }
}