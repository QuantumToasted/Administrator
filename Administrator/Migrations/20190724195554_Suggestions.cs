using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace Administrator.Migrations
{
    public partial class Suggestions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLevelUp",
                table: "GuildUsers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastXpGain",
                table: "GuildUsers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "TotalXp",
                table: "GuildUsers",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLevelUp",
                table: "GlobalUsers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastXpGain",
                table: "GlobalUsers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "TotalXp",
                table: "GlobalUsers",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SpecialEmotes",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Emote = table.Column<string>(nullable: true, defaultValueSql: "''")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialEmotes", x => new { x.GuildId, x.Type });
                });

            migrationBuilder.CreateTable(
                name: "Suggestions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    Text = table.Column<string>(nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(nullable: false),
                    MessageId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suggestions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecialEmotes");

            migrationBuilder.DropTable(
                name: "Suggestions");

            migrationBuilder.DropColumn(
                name: "LastLevelUp",
                table: "GuildUsers");

            migrationBuilder.DropColumn(
                name: "LastXpGain",
                table: "GuildUsers");

            migrationBuilder.DropColumn(
                name: "TotalXp",
                table: "GuildUsers");

            migrationBuilder.DropColumn(
                name: "LastLevelUp",
                table: "GlobalUsers");

            migrationBuilder.DropColumn(
                name: "LastXpGain",
                table: "GlobalUsers");

            migrationBuilder.DropColumn(
                name: "TotalXp",
                table: "GlobalUsers");
        }
    }
}
