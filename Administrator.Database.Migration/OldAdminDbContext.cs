using Microsoft.EntityFrameworkCore;

namespace Administrator.Database.Migration
{
    public partial class OldAdminDbContext : DbContext
    {
        public OldAdminDbContext()
        { }

        public OldAdminDbContext(DbContextOptions<OldAdminDbContext> options)
            : base(options)
        { }

        public virtual DbSet<BigEmoji> BigEmojis { get; set; } = null!;
        public virtual DbSet<CommandAlias> CommandAliases { get; set; } = null!;
        public virtual DbSet<Cooldown> Cooldowns { get; set; } = null!;
        public virtual DbSet<CooldownDatum> CooldownData { get; set; } = null!;
        public virtual DbSet<GlobalUser> GlobalUsers { get; set; } = null!;
        public virtual DbSet<Guild> Guilds { get; set; } = null!;
        public virtual DbSet<GuildUser> GuildUsers { get; set; } = null!;
        public virtual DbSet<Highlight> Highlights { get; set; } = null!;
        public virtual DbSet<LevelReward> LevelRewards { get; set; } = null!;
        public virtual DbSet<LoggingChannel> LoggingChannels { get; set; } = null!;
        public virtual DbSet<MessageFilter> MessageFilters { get; set; } = null!;
        public virtual DbSet<Modmail> Modmails { get; set; } = null!;
        public virtual DbSet<ModmailMessage> ModmailMessages { get; set; } = null!;
        public virtual DbSet<Permission> Permissions { get; set; } = null!;
        public virtual DbSet<Punishment> Punishments { get; set; } = null!;
        public virtual DbSet<ReactionRole> ReactionRoles { get; set; } = null!;
        public virtual DbSet<Reminder> Reminders { get; set; } = null!;
        public virtual DbSet<SelfRole> SelfRoles { get; set; } = null!;
        public virtual DbSet<SpecialEmoji> SpecialEmojis { get; set; } = null!;
        public virtual DbSet<SpecialRole> SpecialRoles { get; set; } = null!;
        public virtual DbSet<Starboard> Starboards { get; set; } = null!;
        public virtual DbSet<Status> Statuses { get; set; } = null!;
        public virtual DbSet<Suggestion> Suggestions { get; set; } = null!;
        public virtual DbSet<Tag> Tags { get; set; } = null!;
        public virtual DbSet<TextChannel> TextChannels { get; set; } = null!;
        public virtual DbSet<WarningPunishment> WarningPunishments { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Trust Server Certificate=True;Persist Security Info=True;Password=w2#fjk&w;Username=admin;Database=admin;Host=192.168.88.101");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BigEmoji>(entity =>
            {
                entity.Property(e => e.Id).HasPrecision(20);

                entity.Property(e => e.DenierId).HasPrecision(20);

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.RequesterId).HasPrecision(20);
            });

            modelBuilder.Entity<CommandAlias>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.Alias });

                entity.Property(e => e.GuildId).HasPrecision(20);
            });

            modelBuilder.Entity<Cooldown>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.CommandName });

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.Cooldown1).HasColumnName("Cooldown");
            });

            modelBuilder.Entity<CooldownDatum>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.UserId, e.Command });

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.UserId).HasPrecision(20);
            });

            modelBuilder.Entity<GlobalUser>(entity =>
            {
                entity.Property(e => e.Id).HasPrecision(20);

                entity.Property(e => e.HighlightBlacklist).HasDefaultValueSql("''::text");

                entity.Property(e => e.PreviousNames).HasDefaultValueSql("'{}'::text[]");
            });

            modelBuilder.Entity<Guild>(entity =>
            {
                entity.Property(e => e.Id).HasPrecision(20);

                entity.Property(e => e.BlacklistedMessageFilterIds).HasDefaultValueSql("''::text");

                entity.Property(e => e.BlacklistedModmailAuthors).HasDefaultValueSql("''::text");

                entity.Property(e => e.BlacklistedStarboardIds).HasDefaultValueSql("''::text");

                entity.Property(e => e.CustomPrefixes).HasDefaultValueSql("'{}'::text[]");
            });

            modelBuilder.Entity<GuildUser>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.GuildId });

                entity.Property(e => e.Id).HasPrecision(20);

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.PreviousNames).HasDefaultValueSql("'{}'::text[]");
            });

            modelBuilder.Entity<Highlight>(entity =>
            {
                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.UserId).HasPrecision(20);
            });

            modelBuilder.Entity<LevelReward>(entity =>
            {
                entity.Property(e => e.AddedRoleIds).HasDefaultValueSql("''::text");

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.RemovedRoleIds).HasDefaultValueSql("''::text");
            });

            modelBuilder.Entity<LoggingChannel>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.Type });

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.Id).HasPrecision(20);
            });

            modelBuilder.Entity<MessageFilter>(entity =>
            {
                entity.Property(e => e.GuildId).HasPrecision(20);
            });

            modelBuilder.Entity<Modmail>(entity =>
            {
                entity.ToTable("Modmail");

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.UserId).HasPrecision(20);
            });

            modelBuilder.Entity<ModmailMessage>(entity =>
            {
                entity.HasIndex(e => e.SourceId, "IX_ModmailMessages_SourceId");

                entity.HasOne(d => d.Source)
                    .WithMany(p => p.ModmailMessages)
                    .HasForeignKey(d => d.SourceId);
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.TargetId).HasPrecision(20);
            });

            modelBuilder.Entity<Punishment>(entity =>
            {
                entity.Property(e => e.ChannelId).HasPrecision(20);

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.LogMessageChannelId).HasPrecision(20);

                entity.Property(e => e.LogMessageId).HasPrecision(20);

                entity.Property(e => e.ModeratorId).HasPrecision(20);

                entity.Property(e => e.PreviousChannelAllowValue).HasPrecision(20);

                entity.Property(e => e.PreviousChannelDenyValue).HasPrecision(20);

                entity.Property(e => e.RevokerId).HasPrecision(20);

                entity.Property(e => e.TargetId).HasPrecision(20);
            });

            modelBuilder.Entity<ReactionRole>(entity =>
            {
                entity.Property(e => e.ChannelId).HasPrecision(20);

                entity.Property(e => e.Emoji).HasDefaultValueSql("''::text");

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.MessageId).HasPrecision(20);

                entity.Property(e => e.RoleId).HasPrecision(20);
            });

            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.Property(e => e.AuthorId).HasPrecision(20);

                entity.Property(e => e.ChannelId).HasPrecision(20);

                entity.Property(e => e.MessageId).HasPrecision(20);
            });

            modelBuilder.Entity<SelfRole>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.RoleId });

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.RoleId).HasPrecision(20);
            });

            modelBuilder.Entity<SpecialEmoji>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.Type });

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.Emoji).HasDefaultValueSql("''::text");
            });

            modelBuilder.Entity<SpecialRole>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.Type });

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.Id).HasPrecision(20);
            });

            modelBuilder.Entity<Starboard>(entity =>
            {
                entity.HasKey(e => e.MessageId);

                entity.ToTable("Starboard");

                entity.Property(e => e.MessageId).HasPrecision(20);

                entity.Property(e => e.AuthorId).HasPrecision(20);

                entity.Property(e => e.ChannelId).HasPrecision(20);

                entity.Property(e => e.EntryChannelId).HasPrecision(20);

                entity.Property(e => e.EntryMessageId).HasPrecision(20);

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.Stars).HasDefaultValueSql("''::text");
            });

            modelBuilder.Entity<Suggestion>(entity =>
            {
                entity.Property(e => e.AuthorId).HasPrecision(20);

                entity.Property(e => e.ChannelId).HasPrecision(20);

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.MessageId).HasPrecision(20);

                entity.Property(e => e.ModifierId).HasPrecision(20);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.Name });

                entity.Property(e => e.GuildId).HasPrecision(20);

                entity.Property(e => e.AuthorId).HasPrecision(20);
            });

            modelBuilder.Entity<TextChannel>(entity =>
            {
                entity.Property(e => e.Id).HasPrecision(20);
            });

            modelBuilder.Entity<WarningPunishment>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.Count });

                entity.Property(e => e.GuildId).HasPrecision(20);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
