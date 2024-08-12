using System.ComponentModel.DataAnnotations;

namespace Administrator.Core;

public sealed class AdministratorSteamConfiguration : IAdministratorConfiguration<AdministratorSteamConfiguration>
{
    [Required]
    public string ApiKey { get; init; } = null!;
}