using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class BlacklistedEmojiGuilds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlacklistedEmojiGuilds",
                table: "Guilds",
                nullable: true,
                defaultValueSql: "''");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlacklistedEmojiGuilds",
                table: "Guilds");
        }
    }
}
