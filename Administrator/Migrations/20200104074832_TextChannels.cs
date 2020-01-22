using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class TextChannels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TextChannels",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    ChannelId = table.Column<decimal>(nullable: false),
                    Settings = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextChannels", x => new { x.GuildId, x.ChannelId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TextChannels");
        }
    }
}
