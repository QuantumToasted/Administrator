using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class Greetings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DmGreeting",
                table: "Guilds",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Greeting",
                table: "Guilds",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "GreetingDuration",
                table: "Guilds",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DmGreeting",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "Greeting",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "GreetingDuration",
                table: "Guilds");
        }
    }
}
