using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Administrator.Migrations
{
    public partial class Reset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalUsers",
                columns: table => new
                {
                    Id = table.Column<decimal>(nullable: false),
                    PreviousNames = table.Column<List<string>>(nullable: true, defaultValueSql: "'{}'"),
                    Language = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<decimal>(nullable: false),
                    Language = table.Column<string>(nullable: true),
                    CustomPrefixes = table.Column<List<string>>(nullable: true, defaultValueSql: "'{}'"),
                    BlacklistedModmailAuthors = table.Column<string>(nullable: true, defaultValueSql: "''"),
                    Settings = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildUsers",
                columns: table => new
                {
                    Id = table.Column<decimal>(nullable: false),
                    GuildId = table.Column<decimal>(nullable: false),
                    PreviousNames = table.Column<List<string>>(nullable: true, defaultValueSql: "'{}'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildUsers", x => new { x.Id, x.GuildId });
                });

            migrationBuilder.CreateTable(
                name: "LoggingChannels",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Id = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoggingChannels", x => new { x.GuildId, x.Type });
                });

            migrationBuilder.CreateTable(
                name: "Modmails",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    IsAnonymous = table.Column<bool>(nullable: false),
                    ClosedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modmails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    GuildId = table.Column<decimal>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    IsEnabled = table.Column<bool>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Filter = table.Column<int>(nullable: false),
                    TargetId = table.Column<decimal>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Punishments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    GuildId = table.Column<decimal>(nullable: false),
                    TargetId = table.Column<decimal>(nullable: false),
                    ModeratorId = table.Column<decimal>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    LogMessageId = table.Column<decimal>(nullable: false),
                    LogMessageChannelId = table.Column<decimal>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    IsAppealable = table.Column<bool>(nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(nullable: true),
                    RevokerId = table.Column<decimal>(nullable: true),
                    RevocationReason = table.Column<string>(nullable: true),
                    AppealedAt = table.Column<DateTimeOffset>(nullable: true),
                    AppealReason = table.Column<string>(nullable: true),
                    Duration = table.Column<TimeSpan>(nullable: true),
                    Mute_Duration = table.Column<TimeSpan>(nullable: true),
                    ChannelId = table.Column<decimal>(nullable: true),
                    PreviousChannelAllowValue = table.Column<decimal>(nullable: true),
                    PreviousChannelDenyValue = table.Column<decimal>(nullable: true),
                    SecondaryPunishmentId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Punishments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecialRoles",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Id = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialRoles", x => new { x.GuildId, x.Type });
                });

            migrationBuilder.CreateTable(
                name: "WarningPunishments",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    Count = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Duration = table.Column<TimeSpan>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarningPunishments", x => new { x.GuildId, x.Count });
                });

            migrationBuilder.CreateTable(
                name: "ModmailMessages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Target = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true),
                    SourceId = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModmailMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModmailMessages_Modmails_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Modmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModmailMessages_SourceId",
                table: "ModmailMessages",
                column: "SourceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalUsers");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "GuildUsers");

            migrationBuilder.DropTable(
                name: "LoggingChannels");

            migrationBuilder.DropTable(
                name: "ModmailMessages");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Punishments");

            migrationBuilder.DropTable(
                name: "SpecialRoles");

            migrationBuilder.DropTable(
                name: "WarningPunishments");

            migrationBuilder.DropTable(
                name: "Modmails");
        }
    }
}
