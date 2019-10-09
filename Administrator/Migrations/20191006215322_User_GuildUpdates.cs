using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class User_GuildUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LevelUpEmote",
                table: "Guilds",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LevelUpWhitelist",
                table: "Guilds",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "XpGainInterval",
                table: "Guilds",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "XpRate",
                table: "Guilds",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LevelUpPreferences",
                table: "GlobalUsers",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LevelUpEmote",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "LevelUpWhitelist",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "XpGainInterval",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "XpRate",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "LevelUpPreferences",
                table: "GlobalUsers");
        }
    }
}
