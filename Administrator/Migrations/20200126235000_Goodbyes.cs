using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class Goodbyes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Goodbye",
                table: "Guilds",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "GoodbyeDuration",
                table: "Guilds",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Goodbye",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "GoodbyeDuration",
                table: "Guilds");
        }
    }
}
