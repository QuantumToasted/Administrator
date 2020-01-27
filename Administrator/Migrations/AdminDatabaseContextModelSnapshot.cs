﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Administrator.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Administrator.Migrations
{
    [DbContext(typeof(AdminDatabaseContext))]
    partial class AdminDatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.4-servicing-10062")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Administrator.Database.CommandAlias", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("Alias");

                    b.Property<string>("Command");

                    b.HasKey("GuildId", "Alias");

                    b.ToTable("CommandAliases");
                });

            modelBuilder.Entity("Administrator.Database.CommandCooldown", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("CommandName");

                    b.Property<TimeSpan>("Cooldown");

                    b.HasKey("GuildId", "CommandName");

                    b.ToTable("Cooldowns");
                });

            modelBuilder.Entity("Administrator.Database.CooldownData", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("UserId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("Command");

                    b.Property<DateTimeOffset>("LastRun");

                    b.HasKey("GuildId", "UserId", "Command");

                    b.ToTable("CooldownData");
                });

            modelBuilder.Entity("Administrator.Database.CyclingStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Text");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.ToTable("Statuses");
                });

            modelBuilder.Entity("Administrator.Database.GlobalUser", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("HighlightBlacklist")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("''");

                    b.Property<string>("Language");

                    b.Property<DateTimeOffset>("LastLevelUp");

                    b.Property<DateTimeOffset>("LastXpGain");

                    b.Property<int>("LevelUpPreferences");

                    b.Property<List<string>>("PreviousNames")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'{}'");

                    b.Property<int>("TotalXp");

                    b.HasKey("Id");

                    b.ToTable("GlobalUsers");
                });

            modelBuilder.Entity("Administrator.Database.Guild", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int>("BigEmojiSize");

                    b.Property<string>("BlacklistedEmojiGuilds")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("''");

                    b.Property<string>("BlacklistedModmailAuthors")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("''");

                    b.Property<string>("BlacklistedStarboardIds")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("''");

                    b.Property<List<string>>("CustomPrefixes")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'{}'");

                    b.Property<bool>("DmGreeting");

                    b.Property<string>("Goodbye");

                    b.Property<TimeSpan?>("GoodbyeDuration");

                    b.Property<string>("Greeting");

                    b.Property<TimeSpan?>("GreetingDuration");

                    b.Property<string>("Language");

                    b.Property<int>("LevelUpWhitelist");

                    b.Property<int>("MaximumReactionRoles");

                    b.Property<int>("MinimumStars");

                    b.Property<int>("Settings");

                    b.Property<TimeSpan>("XpGainInterval");

                    b.Property<int>("XpRate");

                    b.HasKey("Id");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("Administrator.Database.GuildUser", b =>
                {
                    b.Property<decimal>("Id")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<DateTimeOffset>("LastLevelUp");

                    b.Property<DateTimeOffset>("LastXpGain");

                    b.Property<List<string>>("PreviousNames")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("'{}'");

                    b.Property<int>("TotalXp");

                    b.HasKey("Id", "GuildId");

                    b.ToTable("GuildUsers");
                });

            modelBuilder.Entity("Administrator.Database.Highlight", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal?>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("Text");

                    b.Property<decimal>("UserId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.HasKey("Id");

                    b.ToTable("Highlights");
                });

            modelBuilder.Entity("Administrator.Database.LevelReward", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Discriminator")
                        .IsRequired();

                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int>("Level");

                    b.Property<int>("Tier");

                    b.HasKey("Id");

                    b.ToTable("LevelRewards");

                    b.HasDiscriminator<string>("Discriminator").HasValue("LevelReward");
                });

            modelBuilder.Entity("Administrator.Database.LoggingChannel", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int>("Type");

                    b.Property<decimal>("Id")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.HasKey("GuildId", "Type");

                    b.ToTable("LoggingChannels");
                });

            modelBuilder.Entity("Administrator.Database.Modmail", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("ClosedBy");

                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<bool>("IsAnonymous");

                    b.Property<decimal>("UserId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.HasKey("Id");

                    b.ToTable("Modmails");
                });

            modelBuilder.Entity("Administrator.Database.ModmailMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("SourceId");

                    b.Property<int>("Target");

                    b.Property<string>("Text");

                    b.Property<DateTimeOffset>("Timestamp");

                    b.HasKey("Id");

                    b.HasIndex("SourceId");

                    b.ToTable("ModmailMessages");
                });

            modelBuilder.Entity("Administrator.Database.Permission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Filter");

                    b.Property<decimal?>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<bool>("IsEnabled");

                    b.Property<string>("Name");

                    b.Property<decimal?>("TargetId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.ToTable("Permissions");
                });

            modelBuilder.Entity("Administrator.Database.Punishment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTimeOffset>("CreatedAt");

                    b.Property<string>("Discriminator")
                        .IsRequired();

                    b.Property<byte>("Format");

                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<byte[]>("Image");

                    b.Property<decimal>("LogMessageChannelId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("LogMessageId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("ModeratorId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("Reason");

                    b.Property<decimal>("TargetId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.HasKey("Id");

                    b.ToTable("Punishments");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Punishment");
                });

            modelBuilder.Entity("Administrator.Database.ReactionRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("ChannelId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("Emoji");

                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("MessageId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("RoleId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.HasKey("Id");

                    b.ToTable("ReactionRoles");
                });

            modelBuilder.Entity("Administrator.Database.Reminder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("AuthorId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("ChannelId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<DateTimeOffset>("CreatedAt");

                    b.Property<DateTimeOffset>("Ending");

                    b.Property<decimal?>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("MessageId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("Text");

                    b.HasKey("Id");

                    b.ToTable("Reminders");
                });

            modelBuilder.Entity("Administrator.Database.SelfAssignableRole", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("RoleId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int[]>("Groups");

                    b.HasKey("GuildId", "RoleId");

                    b.ToTable("SelfRoles");
                });

            modelBuilder.Entity("Administrator.Database.SpecialEmoji", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int>("Type");

                    b.Property<string>("Emoji")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("''");

                    b.HasKey("GuildId", "Type");

                    b.ToTable("SpecialEmojis");
                });

            modelBuilder.Entity("Administrator.Database.SpecialRole", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int>("Type");

                    b.Property<decimal>("Id")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.HasKey("GuildId", "Type");

                    b.ToTable("SpecialRoles");
                });

            modelBuilder.Entity("Administrator.Database.StarboardEntry", b =>
                {
                    b.Property<decimal>("MessageId")
                        .ValueGeneratedOnAdd()
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("AuthorId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("ChannelId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("EntryChannelId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("EntryMessageId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("Stars")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("''");

                    b.HasKey("MessageId");

                    b.ToTable("Starboard");
                });

            modelBuilder.Entity("Administrator.Database.Suggestion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<byte>("Format");

                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<byte[]>("Image");

                    b.Property<decimal>("MessageId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("Text");

                    b.Property<DateTimeOffset>("Timestamp");

                    b.Property<decimal>("UserId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.HasKey("Id");

                    b.ToTable("Suggestions");
                });

            modelBuilder.Entity("Administrator.Database.Tag", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<string>("Name");

                    b.Property<decimal>("AuthorId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<DateTimeOffset>("CreatedAt");

                    b.Property<byte>("Format");

                    b.Property<byte[]>("Image");

                    b.Property<string>("Response");

                    b.Property<int>("Uses");

                    b.HasKey("GuildId", "Name");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("Administrator.Database.TextChannel", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal>("ChannelId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int>("Settings");

                    b.HasKey("GuildId", "ChannelId");

                    b.ToTable("TextChannels");
                });

            modelBuilder.Entity("Administrator.Database.WarningPunishment", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int>("Count");

                    b.Property<TimeSpan?>("Duration");

                    b.Property<int>("Type");

                    b.HasKey("GuildId", "Count");

                    b.ToTable("WarningPunishments");
                });

            modelBuilder.Entity("Administrator.Database.RoleLevelReward", b =>
                {
                    b.HasBaseType("Administrator.Database.LevelReward");

                    b.Property<string>("AddedRoleIds")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("''");

                    b.Property<string>("RemovedRoleIds")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("''");

                    b.HasDiscriminator().HasValue("RoleLevelReward");
                });

            modelBuilder.Entity("Administrator.Database.Kick", b =>
                {
                    b.HasBaseType("Administrator.Database.Punishment");

                    b.HasDiscriminator().HasValue("Kick");
                });

            modelBuilder.Entity("Administrator.Database.RevocablePunishment", b =>
                {
                    b.HasBaseType("Administrator.Database.Punishment");

                    b.Property<string>("AppealReason");

                    b.Property<DateTimeOffset?>("AppealedAt");

                    b.Property<bool>("IsAppealable");

                    b.Property<string>("RevocationReason");

                    b.Property<DateTimeOffset?>("RevokedAt");

                    b.Property<decimal>("RevokerId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.HasDiscriminator().HasValue("RevocablePunishment");
                });

            modelBuilder.Entity("Administrator.Database.Ban", b =>
                {
                    b.HasBaseType("Administrator.Database.RevocablePunishment");

                    b.Property<TimeSpan?>("Duration");

                    b.HasDiscriminator().HasValue("Ban");
                });

            modelBuilder.Entity("Administrator.Database.Mute", b =>
                {
                    b.HasBaseType("Administrator.Database.RevocablePunishment");

                    b.Property<decimal?>("ChannelId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<TimeSpan?>("Duration")
                        .HasColumnName("Mute_Duration");

                    b.Property<decimal?>("PreviousChannelAllowValue")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<decimal?>("PreviousChannelDenyValue")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.HasDiscriminator().HasValue("Mute");
                });

            modelBuilder.Entity("Administrator.Database.Warning", b =>
                {
                    b.HasBaseType("Administrator.Database.RevocablePunishment");

                    b.Property<int?>("SecondaryPunishmentId");

                    b.HasDiscriminator().HasValue("Warning");
                });

            modelBuilder.Entity("Administrator.Database.ModmailMessage", b =>
                {
                    b.HasOne("Administrator.Database.Modmail", "Source")
                        .WithMany("Messages")
                        .HasForeignKey("SourceId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
