using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Administrator.Database;

public class AdminDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables("ADMIN_BETA_")
            .Build();

        return new AdminDbContext(new DbContextOptionsBuilder<AdminDbContext>()
            .UseNpgsql(configuration["DB_CONNECTION_STRING"])
            .UseSnakeCaseNamingConvention()
            .Options);
    }
}