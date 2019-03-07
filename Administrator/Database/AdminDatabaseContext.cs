using Administrator.Services;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database
{
    public sealed class AdminDatabaseContext : DbContext
    {
        private static readonly ConfigurationService Config = ConfigurationService.Basic;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Config.PostgresConnectionString);
        }
    }
}