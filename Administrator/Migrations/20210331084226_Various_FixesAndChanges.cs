using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class Various_FixesAndChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "special_roles",
                newName: "id");

            migrationBuilder.AddColumn<int>(
                name: "ban_prune_days",
                table: "guilds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "settings",
                table: "guilds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "logging_channel",
                columns: table => new
                {
                    guild_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    id = table.Column<ulong>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logging_channel", x => new { x.guild_id, x.type });
                });

            migrationBuilder.CreateTable(
                name: "warning_punishments",
                columns: table => new
                {
                    guild_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warning_punishments", x => new { x.guild_id, x.count });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logging_channel");

            migrationBuilder.DropTable(
                name: "warning_punishments");

            migrationBuilder.DropColumn(
                name: "ban_prune_days",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "settings",
                table: "guilds");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "special_roles",
                newName: "role_id");
        }
    }
}
