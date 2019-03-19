using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class AdditionalPunishmentsWork_LoggingChannels_SpecialRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LogMessageChannelId",
                table: "Punishments",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LogMessageId",
                table: "Punishments",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "LoggingChannels",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Id = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoggingChannels", x => new { x.GuildId, x.Type });
                });

            migrationBuilder.CreateTable(
                name: "SpecialRoles",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Id = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialRoles", x => new { x.GuildId, x.Type });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoggingChannels");

            migrationBuilder.DropTable(
                name: "SpecialRoles");

            migrationBuilder.DropColumn(
                name: "LogMessageChannelId",
                table: "Punishments");

            migrationBuilder.DropColumn(
                name: "LogMessageId",
                table: "Punishments");
        }
    }
}
