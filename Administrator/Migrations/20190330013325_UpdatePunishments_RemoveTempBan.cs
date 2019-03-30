using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class UpdatePunishments_RemoveTempBan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TemporaryBan_Duration",
                table: "Punishments",
                newName: "Mute_Duration");

            migrationBuilder.AddColumn<decimal>(
                name: "ChannelId",
                table: "Punishments",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Settings",
                table: "Guilds",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "Punishments");

            migrationBuilder.DropColumn(
                name: "Settings",
                table: "Guilds");

            migrationBuilder.RenameColumn(
                name: "Mute_Duration",
                table: "Punishments",
                newName: "TemporaryBan_Duration");
        }
    }
}
