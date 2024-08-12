using System.ComponentModel.DataAnnotations;

namespace Administrator.Core;

public sealed class AdministratorBackblazeConfiguration : IAdministratorConfiguration<AdministratorBackblazeConfiguration>
{
    [Required]
    public string BaseUrl { get; init; } = null!;

    [Required]
    public string KeyId { get; init; } = null!;

    [Required] 
    public string Key { get; init; } = null!;
}