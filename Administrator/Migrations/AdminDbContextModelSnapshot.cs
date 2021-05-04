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
    [DbContext(typeof(AdminDbContext))]
    partial class AdminDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.4")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("Administrator.Database.BigEmoji", b =>
                {
                    b.Property<ulong>("Id")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<bool>("IsAnimated")
                        .HasColumnType("boolean")
                        .HasColumnName("is_animated");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("emoji_type")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("emoji_type");

                    b.HasKey("Id")
                        .HasName("pk_big_emojis");

                    b.ToTable("big_emojis");

                    b.HasDiscriminator<string>("emoji_type").HasValue("BigEmoji");
                });

            modelBuilder.Entity("Administrator.Database.Guild", b =>
                {
                    b.Property<ulong>("Id")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.Property<int?>("BanPruneDays")
                        .HasColumnType("integer")
                        .HasColumnName("ban_prune_days");

                    b.Property<int>("BigEmojiSizeMultiplier")
                        .HasColumnType("integer")
                        .HasColumnName("big_emoji_size_multiplier");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<List<string>>("Prefixes")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text[]")
                        .HasColumnName("prefixes")
                        .HasDefaultValueSql("'{}'");

                    b.Property<int>("Settings")
                        .HasColumnType("integer")
                        .HasColumnName("settings");

                    b.HasKey("Id")
                        .HasName("pk_guilds");

                    b.ToTable("guilds");
                });

            modelBuilder.Entity("Administrator.Database.LoggingChannel", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<ulong>("Id")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.HasKey("GuildId", "Type")
                        .HasName("pk_logging_channel");

                    b.ToTable("logging_channel");
                });

            modelBuilder.Entity("Administrator.Database.Punishment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Attachment")
                        .HasColumnType("text")
                        .HasColumnName("attachment");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<ulong>("LogChannelId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("log_channel_id");

                    b.Property<ulong>("LogMessageId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("log_message_id");

                    b.Property<ulong>("ModeratorId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("moderator_id");

                    b.Property<string>("ModeratorTag")
                        .HasColumnType("text")
                        .HasColumnName("moderator_tag");

                    b.Property<string>("Reason")
                        .HasColumnType("text")
                        .HasColumnName("reason");

                    b.Property<ulong>("TargetId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("target_id");

                    b.Property<string>("TargetTag")
                        .HasColumnType("text")
                        .HasColumnName("target_tag");

                    b.Property<string>("punishment_type")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("punishment_type");

                    b.HasKey("Id")
                        .HasName("pk_punishments");

                    b.ToTable("punishments");

                    b.HasDiscriminator<string>("punishment_type").HasValue("Punishment");
                });

            modelBuilder.Entity("Administrator.Database.SpecialEmoji", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<string>("Emoji")
                        .HasColumnType("text")
                        .HasColumnName("emoji");

                    b.HasKey("GuildId", "Type")
                        .HasName("pk_special_emojis");

                    b.ToTable("special_emojis");
                });

            modelBuilder.Entity("Administrator.Database.SpecialRole", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<ulong>("Id")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.HasKey("GuildId", "Type")
                        .HasName("pk_special_roles");

                    b.ToTable("special_roles");
                });

            modelBuilder.Entity("Administrator.Database.WarningPunishment", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int>("Count")
                        .HasColumnType("integer")
                        .HasColumnName("count");

                    b.Property<TimeSpan?>("Duration")
                        .HasColumnType("interval")
                        .HasColumnName("duration");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.HasKey("GuildId", "Count")
                        .HasName("pk_warning_punishments");

                    b.ToTable("warning_punishments");
                });

            modelBuilder.Entity("Administrator.Database.ApprovedBigEmoji", b =>
                {
                    b.HasBaseType("Administrator.Database.BigEmoji");

                    b.Property<DateTimeOffset>("ApprovedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("approved_at");

                    b.Property<ulong>("ApproverId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("approver_id");

                    b.Property<string>("ApproverTag")
                        .HasColumnType("text")
                        .HasColumnName("approver_tag");

                    b.ToTable("big_emojis");

                    b.HasDiscriminator().HasValue("approved");
                });

            modelBuilder.Entity("Administrator.Database.DeniedBigEmoji", b =>
                {
                    b.HasBaseType("Administrator.Database.BigEmoji");

                    b.Property<DateTimeOffset>("DeniedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("denied_at");

                    b.Property<ulong>("DenierId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("denier_id");

                    b.Property<string>("DenierTag")
                        .HasColumnType("text")
                        .HasColumnName("denier_tag");

                    b.ToTable("big_emojis");

                    b.HasDiscriminator().HasValue("denied");
                });

            modelBuilder.Entity("Administrator.Database.RequestedBigEmoji", b =>
                {
                    b.HasBaseType("Administrator.Database.BigEmoji");

                    b.Property<DateTimeOffset>("RequestedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("requested_at");

                    b.Property<ulong>("RequesterId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("requester_id");

                    b.Property<string>("RequesterTag")
                        .HasColumnType("text")
                        .HasColumnName("requester_tag");

                    b.ToTable("big_emojis");

                    b.HasDiscriminator().HasValue("requested");
                });

            modelBuilder.Entity("Administrator.Database.Kick", b =>
                {
                    b.HasBaseType("Administrator.Database.Punishment");

                    b.ToTable("punishments");

                    b.HasDiscriminator().HasValue("kick");
                });

            modelBuilder.Entity("Administrator.Database.RevocablePunishment", b =>
                {
                    b.HasBaseType("Administrator.Database.Punishment");

                    b.Property<string>("AppealReason")
                        .HasColumnType("text")
                        .HasColumnName("appeal_reason");

                    b.Property<DateTimeOffset?>("AppealedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("appealed_at");

                    b.Property<DateTimeOffset?>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<string>("RevocationReason")
                        .HasColumnType("text")
                        .HasColumnName("revocation_reason");

                    b.Property<DateTimeOffset?>("RevokedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("revoked_at");

                    b.Property<ulong>("RevokerId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("revoker_id");

                    b.Property<string>("RevokerTag")
                        .HasColumnType("text")
                        .HasColumnName("revoker_tag");

                    b.ToTable("punishments");

                    b.HasDiscriminator().HasValue("RevocablePunishment");
                });

            modelBuilder.Entity("Administrator.Database.Ban", b =>
                {
                    b.HasBaseType("Administrator.Database.RevocablePunishment");

                    b.ToTable("punishments");

                    b.HasDiscriminator().HasValue("ban");
                });

            modelBuilder.Entity("Administrator.Database.Mute", b =>
                {
                    b.HasBaseType("Administrator.Database.RevocablePunishment");

                    b.Property<ulong?>("ChannelId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("channel_id");

                    b.Property<decimal?>("PreviousChannelAllowValue")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("previous_channel_allow_value");

                    b.Property<decimal?>("PreviousChannelDenyValue")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("previous_channel_deny_value");

                    b.ToTable("punishments");

                    b.HasDiscriminator().HasValue("mute");
                });

            modelBuilder.Entity("Administrator.Database.Warning", b =>
                {
                    b.HasBaseType("Administrator.Database.RevocablePunishment");

                    b.Property<int?>("SecondaryPunishmentId")
                        .HasColumnType("integer")
                        .HasColumnName("secondary_punishment_id");

                    b.ToTable("punishments");

                    b.HasDiscriminator().HasValue("warning");
                });
#pragma warning restore 612, 618
        }
    }
}
