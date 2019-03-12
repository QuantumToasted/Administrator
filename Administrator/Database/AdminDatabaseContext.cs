using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Database
{
    public sealed class AdminDatabaseContext : DbContext
    {
        private static readonly ConfigurationService Config = ConfigurationService.Basic;

        private static readonly IServiceProvider EmptyProvider =
            new ServiceCollection().AddEntityFrameworkNpgsql().BuildServiceProvider();

        private readonly IServiceProvider _provider;

        public AdminDatabaseContext() : this(EmptyProvider)
        { }

        public AdminDatabaseContext(IServiceProvider provider)
        {
            _provider = provider ?? EmptyProvider;
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Config.PostgresConnectionString)
                .UseInternalServiceProvider(_provider);
        }
    }
}