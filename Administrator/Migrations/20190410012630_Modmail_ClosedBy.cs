using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class Modmail_ClosedBy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "Modmails");

            migrationBuilder.AddColumn<int>(
                name: "ClosedBy",
                table: "Modmails",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedBy",
                table: "Modmails");

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "Modmails",
                nullable: false,
                defaultValue: false);
        }
    }
}
