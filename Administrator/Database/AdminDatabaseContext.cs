using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Services;
using Discord.WebSocket;
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
        private readonly DiscordSocketClient _client;
        private readonly LocalizationService _localization;

        public AdminDatabaseContext() : this(null)
        { }

        public AdminDatabaseContext(IServiceProvider provider)
        {
            if (!(provider is null))
            {
                _client = provider.GetRequiredService<DiscordSocketClient>();
                _localization = provider.GetRequiredService<LocalizationService>();
            }

            _provider = provider ?? EmptyProvider;
        }

        public DbSet<Guild> Guilds { get; set; }
        
        public DbSet<GlobalUser> GlobalUsers { get; set; }

        public DbSet<Punishment> Punishments { get; set; }

        public DbSet<LoggingChannel> LoggingChannels { get; set; }

        public DbSet<SpecialRole> SpecialRoles { get; set; }

        public DbSet<Modmail> Modmails { get; set; }

        public DbSet<ModmailMessage> ModmailMessages { get; set; }

        public async Task<Guild> GetOrCreateGuildAsync(ulong guildId)
        {
            if (await Guilds.FindAsync(guildId) is Guild guild)
                return guild;

            guild = new Guild(guildId, _localization);
            Guilds.Add(guild);
            await SaveChangesAsync();
            return guild;
        }
        
        public async Task<GlobalUser> GetOrCreateGlobalUserAsync(ulong userId)
        {
            if (await GlobalUsers.FindAsync(userId) is GlobalUser user)
                return user;

            user = new GlobalUser(userId, _localization);
            GlobalUsers.Add(user);
            await SaveChangesAsync();
            return user;
        }

        public async Task<SocketTextChannel> GetLoggingChannelAsync(ulong guildId, LogType type)
        {
            if (!(await LoggingChannels.FindAsync(guildId, type) is LoggingChannel logChannel))
                return null;

            return _client.GetGuild(guildId).GetTextChannel(logChannel.Id);
        }

        public async Task<SocketRole> GetSpecialRoleAsync(ulong guildId, RoleType type)
        {
            if (!(await SpecialRoles.FindAsync(guildId, type) is SpecialRole role))
                return null;

            return _client.GetGuild(guildId).GetRole(role.Id);
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
                        x => _localization.Languages
                            .First(y => y.CultureCode.Equals(x)));
                guild.Property(x => x.CustomPrefixes).HasDefaultValueSql("'{}'");
                guild.Property(x => x.BlacklistedModmailAuthors)
                    .HasConversion(new SnowflakeCollectionConverter())
                    .HasDefaultValueSql("''");
            });

            modelBuilder.Entity<GlobalUser>(user =>
            {
                user.HasKey(x => x.Id);
                user.Property(x => x.Language)
                    .HasConversion(x => x.CultureCode,
                        x => _localization.Languages
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

            modelBuilder.Entity<Warning>(warning =>
            {
                warning.HasBaseType<RevocablePunishment>();
            });

            modelBuilder.Entity<LoggingChannel>(channel =>
            {
                channel.HasKey(x => new {x.GuildId, x.Type});
            });

            modelBuilder.Entity<SpecialRole>(role =>
            {
                role.HasKey(x => new {x.GuildId, x.Type});
            });

            modelBuilder.Entity<Modmail>(mail =>
            {
                mail.HasKey(x => x.Id);
                mail.Property(x => x.Id).ValueGeneratedOnAdd();
                mail.HasMany(x => x.Messages)
                    .WithOne(x => x.Source)
                    .HasForeignKey(x => x.SourceId);
            });

            modelBuilder.Entity<ModmailMessage>(message =>
            {
                message.HasKey(x => x.Id);
                message.Property(x => x.Id).ValueGeneratedOnAdd();
            });
        }
    }
}