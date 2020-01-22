using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
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
        private readonly DiscordClient _client;
        private readonly LocalizationService _localization;

        public AdminDatabaseContext() 
            : this(null)
        { }

        public AdminDatabaseContext(IServiceProvider provider)
        {
            if (!(provider is null))
            {
                _client = provider.GetRequiredService<DiscordClient>();
                _localization = provider.GetRequiredService<LocalizationService>();
            }

            _provider = provider ?? EmptyProvider;
        }

        public DbSet<Guild> Guilds { get; set; }
        
        public DbSet<GlobalUser> GlobalUsers { get; set; }

        public DbSet<GuildUser> GuildUsers { get; set; }
        
        public DbSet<Punishment> Punishments { get; set; }

        public DbSet<LoggingChannel> LoggingChannels { get; set; }

        public DbSet<SpecialRole> SpecialRoles { get; set; }

        public DbSet<Modmail> Modmails { get; set; }

        public DbSet<ModmailMessage> ModmailMessages { get; set; }

        public DbSet<WarningPunishment> WarningPunishments { get; set; }

        public DbSet<Permission> Permissions { get; set; }

        public DbSet<Suggestion> Suggestions { get; set; }

        public DbSet<SpecialEmoji> SpecialEmojis { get; set; }

        public DbSet<Highlight> Highlights { get; set; }

        public DbSet<ReactionRole> ReactionRoles { get; set; }

        public DbSet<LevelReward> LevelRewards { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public DbSet<Reminder> Reminders { get; set; }

        public DbSet<CommandAlias> CommandAliases { get; set; }

        public DbSet<StarboardEntry> Starboard { get; set; }

        public DbSet<CyclingStatus> Statuses { get; set; }

        public DbSet<TextChannel> TextChannels { get; set; }

        public DbSet<CommandCooldown> Cooldowns { get; set; }

        public DbSet<CooldownData> CooldownData { get; set; }

        public async Task<Guild> GetOrCreateGuildAsync(ulong guildId)
        {
            if (await Guilds.FindAsync(guildId) is { } guild)
                return guild;

            guild = new Guild(guildId, _localization);
            Guilds.Add(guild);
            await SaveChangesAsync();
            return guild;
        }
        
        public async Task<GlobalUser> GetOrCreateGlobalUserAsync(ulong userId)
        {
            if (await GlobalUsers.FindAsync(userId) is { } user)
                return user;

            user = new GlobalUser(userId, _localization);
            GlobalUsers.Add(user);
            await SaveChangesAsync();
            return user;
        }

        public async Task<GuildUser> GetOrCreateGuildUserAsync(ulong userId, ulong guildId)
        {
            if (await GuildUsers.FindAsync(userId, guildId) is { } user)
                return user;

            user = new GuildUser(userId, guildId);
            GuildUsers.Add(user);
            await SaveChangesAsync();
            return user;
        }

        public async Task<CachedTextChannel> GetLoggingChannelAsync(ulong guildId, LogType type)
        {
            if (!(await LoggingChannels.FindAsync(guildId, type) is { } logChannel))
                return null;

            return _client.GetGuild(guildId).GetTextChannel(logChannel.Id);
        }

        public async Task<CachedRole> GetSpecialRoleAsync(ulong guildId, RoleType type)
        {
            if (!(await SpecialRoles.FindAsync(guildId, type) is { } role))
                return null;

            return _client.GetGuild(guildId).GetRole(role.Id);
        }

        public async Task<IEmoji> GetSpecialEmojiAsync(ulong guildId, EmojiType type)
        {
            if (!(await SpecialEmojis.FindAsync(guildId, type) is { } emoji))
                return new LocalEmoji(type.GetDescription());

            return emoji.Emoji;
        }

        public async Task<CachedRole> GetReactionRoleAsync(ulong guildId, ulong messageId, IEmoji emoji)
        {
            if (!(await ReactionRoles.FirstOrDefaultAsync(x =>
                x.GuildId == guildId && x.MessageId == messageId && x.Emoji.Equals(emoji)) is { } reactionRole))
                return null;

            return _client.GetGuild(reactionRole.GuildId).GetRole(reactionRole.RoleId);
        }

        public async Task<TextChannel> GetOrCreateTextChannelAsync(ulong guildId, ulong channelId)
        {
            if (await TextChannels.FindAsync(guildId, channelId) is { } channel)
                return channel;

            channel = new TextChannel(guildId, channelId);
            TextChannels.Add(channel);
            await SaveChangesAsync();
            return channel;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Config.PostgresConnectionString)
                .EnableSensitiveDataLogging()
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
                guild.Property(x => x.BlacklistedEmojiGuilds)
                    .HasConversion(new SnowflakeCollectionConverter())
                    .HasDefaultValueSql("''");
                guild.Property(x => x.BlacklistedStarboardIds)
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
                user.Property(x => x.PreviousNames)
                    .HasDefaultValueSql("'{}'");
                user.Property(x => x.HighlightBlacklist)
                    .HasConversion(new SnowflakeCollectionConverter())
                    .HasDefaultValueSql("''");
            });

            modelBuilder.Entity<GuildUser>(user =>
            {
                user.HasKey(x => new {x.Id, x.GuildId});
                user.Property(x => x.PreviousNames)
                    .HasDefaultValueSql("'{}'");
            });

            modelBuilder.Entity<Punishment>(punishment =>
            {
                punishment.HasKey(x => x.Id);
                punishment.Property(x => x.Id).ValueGeneratedOnAdd();
                punishment.Property(x => x.Image).HasConversion(x => x.ToArray(), x => new MemoryStream(x));
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

            modelBuilder.Entity<WarningPunishment>(punishment =>
            {
                punishment.HasKey(x => new {x.GuildId, x.Count});
            });

            modelBuilder.Entity<Permission>(permission =>
            {
                permission.HasKey(x => x.Id);
                permission.Property(x => x.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Suggestion>(suggestion =>
            {
                suggestion.HasKey(x => x.Id);
                suggestion.Property(x => x.Id).ValueGeneratedOnAdd();
                suggestion.Property(x => x.Image).HasConversion(x => x.ToArray(), x => new MemoryStream(x));
            });

            modelBuilder.Entity<SpecialEmoji>(emoji =>
            {
                emoji.HasKey(x => new {x.GuildId, x.Type});
                emoji.Property(x => x.Emoji).HasConversion(new EmojiConverter())
                    .HasDefaultValueSql("''");
            });

            modelBuilder.Entity<Highlight>(highlight =>
            {
                highlight.HasKey(x => x.Id);
                highlight.Property(x => x.Id)
                    .ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<ReactionRole>(role =>
            {
                role.HasKey(x => x.Id);
                role.Property(x => x.Id)
                    .ValueGeneratedOnAdd();
                role.Property(x => x.Emoji)
                    .HasConversion(x => x.ToString(), x => EmojiTools.Parse(x));
            });

            modelBuilder.Entity<LevelReward>(reward =>
            {
                reward.HasKey(x => x.Id);
                reward.Property(x => x.Id)
                    .ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<RoleLevelReward>(reward =>
            {
                reward.HasBaseType<LevelReward>();
                reward.Property(x => x.AddedRoleIds)
                    .HasConversion(new SnowflakeCollectionConverter())
                    .HasDefaultValueSql("''");
                reward.Property(x => x.RemovedRoleIds)
                    .HasConversion(new SnowflakeCollectionConverter())
                    .HasDefaultValueSql("''");
            });

            modelBuilder.Entity<Tag>(tag =>
            {
                tag.HasKey(x => new {x.GuildId, x.Name});
                tag.Property(x => x.Image).HasConversion(x => x.ToArray(), x => new MemoryStream(x));
            });

            modelBuilder.Entity<Reminder>(reminder =>
            {
                reminder.HasKey(x => x.Id);
                reminder.Property(x => x.Id)
                    .ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<CommandAlias>(alias =>
            {
                alias.HasKey(x => new {x.GuildId, x.Alias});
            });

            modelBuilder.Entity<StarboardEntry>(entry =>
            {
                entry.HasKey(x => x.MessageId);
                entry.Property(x => x.Stars)
                    .HasConversion(new SnowflakeCollectionConverter())
                    .HasDefaultValueSql("''");
            });

            modelBuilder.Entity<CyclingStatus>(status =>
            {
                status.HasKey(x => x.Id);
                status.Property(x => x.Id)
                    .ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TextChannel>(channel =>
            {
                channel.HasKey(x => new {x.GuildId, x.ChannelId});
            });

            modelBuilder.Entity<CommandCooldown>(cooldown =>
            {
                cooldown.HasKey(x => new {x.GuildId, x.CommandName});
            });

            modelBuilder.Entity<CooldownData>(data =>
            {
                data.HasKey(x => new {x.GuildId, x.UserId, x.Command});
            });
        }
    }
}