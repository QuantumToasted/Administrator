using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Administrator.Database
{
    public class AdminDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
    {
        public AdminDesignTimeDbContextFactory()
        {
#if !MIGRATION_MODE
            throw new InvalidOperationException("MIGRATION_MODE must be defined to properly use migrations.");
#endif
        }
        
        public AdminDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables("ADMIN_")
                .Build();

            return new AdminDbContext(default, default, default,
                new DbContextOptionsBuilder().UseNpgsql(configuration["DB_CONNECTION_STRING"]).Options);
        }
    }
}