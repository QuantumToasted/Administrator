using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

public sealed class AdminDbContext : DbContext
{
    internal DbSet<GuildConfiguration> GuildConfigurations { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
    }
}