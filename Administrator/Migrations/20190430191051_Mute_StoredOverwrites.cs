using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class Mute_StoredOverwrites : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PreviousChannelAllowValue",
                table: "Punishments",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousChannelDenyValue",
                table: "Punishments",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousChannelAllowValue",
                table: "Punishments");

            migrationBuilder.DropColumn(
                name: "PreviousChannelDenyValue",
                table: "Punishments");
        }
    }
}
