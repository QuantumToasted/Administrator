using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class FixConstructors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "expires",
                table: "punishments",
                newName: "expires_at");

            migrationBuilder.AddColumn<int>(
                name: "big_emoji_size_multiplier",
                table: "guilds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_at",
                table: "big_emojis",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "big_emoji_size_multiplier",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "big_emojis");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "punishments",
                newName: "expires");
        }
    }
}
