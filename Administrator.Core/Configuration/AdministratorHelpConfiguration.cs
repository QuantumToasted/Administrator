using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Disqord;

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