using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Disqord;

namespace Administrator.Core;

public sealed class AdministratorBotConfiguration : IAdministratorConfiguration<AdministratorBotConfiguration>
{
    [Required]
    public string Token { get; init; } = null!;

    [Required]
    public ulong OwnerModuleGuildId { get; init; }
}