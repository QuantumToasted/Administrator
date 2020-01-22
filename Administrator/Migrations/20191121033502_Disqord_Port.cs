using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class Disqord_Port : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecialEmotes");

            migrationBuilder.DropColumn(
                name: "LevelUpEmote",
                table: "Guilds");

            migrationBuilder.AddColumn<string>(
                name: "LevelUpEmoji",
                table: "Guilds",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SpecialEmojis",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Emoji = table.Column<string>(nullable: true, defaultValueSql: "''")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialEmojis", x => new { x.GuildId, x.Type });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecialEmojis");

            migrationBuilder.DropColumn(
                name: "LevelUpEmoji",
                table: "Guilds");

            migrationBuilder.AddColumn<string>(
                name: "LevelUpEmote",
                table: "Guilds",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SpecialEmotes",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Emote = table.Column<string>(type: "text", nullable: true, defaultValueSql: "''")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialEmotes", x => new { x.GuildId, x.Type });
                });
        }
    }
}
