using System.ComponentModel.DataAnnotations;

namespace Administrator.Core;

public sealed class AdministratorAppealConfiguration : IAdministratorConfiguration<AdministratorAppealConfiguration>
{
    [Required]
    public ulong GuildId { get; init; }

    public string GuildInviteCode { get; init; } = null!;
}