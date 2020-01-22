using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class StarboardFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Stars",
                table: "Starboard",
                nullable: true,
                defaultValueSql: "''",
                oldClrType: typeof(string),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Stars",
                table: "Starboard",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true,
                oldDefaultValueSql: "''");
        }
    }
}
