using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class Starboard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlacklistedStarboardIds",
                table: "Guilds",
                nullable: true,
                defaultValueSql: "''");

            migrationBuilder.AddColumn<int>(
                name: "MinimumStars",
                table: "Guilds",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Starboard",
                columns: table => new
                {
                    MessageId = table.Column<decimal>(nullable: false),
                    ChannelId = table.Column<decimal>(nullable: false),
                    GuildId = table.Column<decimal>(nullable: false),
                    AuthorId = table.Column<decimal>(nullable: false),
                    Stars = table.Column<string>(nullable: true),
                    EntryMessageId = table.Column<decimal>(nullable: false),
                    EntryChannelId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Starboard", x => x.MessageId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Starboard");

            migrationBuilder.DropColumn(
                name: "BlacklistedStarboardIds",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "MinimumStars",
                table: "Guilds");
        }
    }
}
