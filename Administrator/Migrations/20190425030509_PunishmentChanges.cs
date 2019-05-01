using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class PunishmentChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecondaryPunishmentId",
                table: "Punishments",
                nullable: true);

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarningPunishments");

            migrationBuilder.DropColumn(
                name: "SecondaryPunishmentId",
                table: "Punishments");
        }
    }
}
