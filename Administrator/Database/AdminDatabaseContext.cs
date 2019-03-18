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
        
        public DbSet<Guild> Guilds { get; set; }
        
        public DbSet<GlobalUser> GlobalUsers { get; set; }

        public DbSet<Punishment> Punishments { get; set; }

        public async Task<Guild> GetOrCreateGuildAsync(ulong guildId)
        {
            if (await Guilds.FindAsync(guildId) is Guild guild)
                return guild;

            guild = new Guild(guildId, _provider.GetRequiredService<LocalizationService>());
            Guilds.Add(guild);
            await SaveChangesAsync();
            return guild;
        }
        
        public async Task<GlobalUser> GetOrCreateGlobalUserAsync(ulong userId)
        {
            if (await GlobalUsers.FindAsync(userId) is GlobalUser user)
                return user;

            user = new GlobalUser(userId, _provider.GetRequiredService<LocalizationService>());
            GlobalUsers.Add(user);
            await SaveChangesAsync();
            return user;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Config.PostgresConnectionString)
                .UseInternalServiceProvider(_provider);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Guild>(guild =>
            {
                guild.HasKey(x => x.Id);
                guild.Property(x => x.Language)
                    .HasConversion(x => x.CultureCode,
                        x => _provider.GetRequiredService<LocalizationService>().Languages
                            .First(y => y.CultureCode.Equals(x)));
                guild.Property(x => x.CustomPrefixes).HasDefaultValueSql("'{}'");
            });

            modelBuilder.Entity<GlobalUser>(user =>
            {
                user.HasKey(x => x.Id);
                user.Property(x => x.Language)
                    .HasConversion(x => x.CultureCode,
                        x => _provider.GetRequiredService<LocalizationService>().Languages
                            .First(y => y.CultureCode.Equals(x)));
            });

            modelBuilder.Entity<Punishment>(punishment =>
            {
                punishment.HasKey(x => x.Id);
                punishment.Property(x => x.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Kick>(kick =>
            {
                kick.HasBaseType<Punishment>();
            });

            modelBuilder.Entity<RevocablePunishment>(punishment =>
            {
                punishment.HasBaseType<Punishment>();
            });

            modelBuilder.Entity<Ban>(ban =>
            {
                ban.HasBaseType<RevocablePunishment>();
            });

            modelBuilder.Entity<Mute>(mute =>
            {
                mute.HasBaseType<RevocablePunishment>();
            });

            modelBuilder.Entity<TemporaryBan>(tempBan =>
            {
                tempBan.HasBaseType<RevocablePunishment>();
            });

            modelBuilder.Entity<Warning>(warning =>
            {
                warning.HasBaseType<RevocablePunishment>();
            });
        }
    }
}