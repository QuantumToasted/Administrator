using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class CommandCooldowns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cooldowns",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    CommandName = table.Column<string>(nullable: false),
                    Cooldown = table.Column<TimeSpan>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cooldowns", x => new { x.GuildId, x.CommandName });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cooldowns");
        }
    }
}
