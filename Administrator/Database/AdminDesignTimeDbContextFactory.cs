using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Administrator.Database
{
    public class AdminDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
    {
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