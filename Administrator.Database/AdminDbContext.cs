using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

public sealed class AdminDbContext : DbContext
{
    internal DbSet<GuildConfigurationModel> GuildConfigurations { get; init; } = null!;

    internal DbSet<PermissionsPlusModel> Permissions { get; init; } = null!;

    internal DbSet<GuildBlacklistedChannelModel> GuildBlacklistedChannels { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
    }
}