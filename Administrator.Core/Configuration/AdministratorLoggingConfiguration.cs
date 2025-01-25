using System.ComponentModel.DataAnnotations;
using Serilog.Events;

namespace Administrator.Core;

public sealed class AdministratorLoggingConfiguration : IAdministratorConfiguration<AdministratorLoggingConfiguration>
{
    [Required]
    public LogEventLevel DefaultLevel { get; init; }

    public Dictionary<string, LogEventLevel> Overrides { get; init; } = new();
}