using System.ComponentModel.DataAnnotations;

namespace Administrator.Core;

public sealed class AdministratorHelpConfiguration : IAdministratorConfiguration<AdministratorHelpConfiguration>
{
    [Required]
    public string WikiUrl { get; init; } = null!;
    
    [Required]
    public ulong SupportGuildId { get; init; }

    [Required]
    public string SupportGuildInviteCode { get; init; } = null!;
}