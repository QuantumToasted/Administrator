using Administrator.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Administrator.Database;

public sealed class AdminDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var directory = Path.GetFullPath(@"..\Administrator");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile("config.json")
            .Build();

        var config = new AdministratorDatabaseConfiguration();
        
        configuration.GetSection(IAdministratorConfiguration<AdministratorDatabaseConfiguration>.SectionName)
            .Bind(config);

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(config.ConnectionString).EnableDynamicJson();

        return new AdminDbContext(new DbContextOptionsBuilder<AdminDbContext>()
            .UseNpgsql(dataSourceBuilder.Build()).UseSnakeCaseNamingConvention()
            .Options);

        /*
        return new AdminDbContext(new DbContextOptionsBuilder<AdminDbContext>()
            .UseNpgsql(configuration["DB_CONNECTION_STRING"])
            .Options);
        */
    }
}