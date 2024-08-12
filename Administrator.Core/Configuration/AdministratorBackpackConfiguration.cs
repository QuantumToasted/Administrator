using System.ComponentModel.DataAnnotations;

namespace Administrator.Core;

public sealed class AdministratorBackpackConfiguration : IAdministratorConfiguration<AdministratorBackpackConfiguration>
{
    [Required] 
    public string ApiKey { get; init; } = null!;
}