using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class CooldownData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CooldownData",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    Command = table.Column<string>(nullable: false),
                    LastRun = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CooldownData", x => new { x.GuildId, x.UserId, x.Command });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CooldownData");
        }
    }
}
