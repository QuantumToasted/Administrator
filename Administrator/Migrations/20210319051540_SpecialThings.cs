using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class SpecialThings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "special_emojis",
                columns: table => new
                {
                    guild_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    emoji = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_special_emojis", x => new { x.guild_id, x.type });
                });

            migrationBuilder.CreateTable(
                name: "special_roles",
                columns: table => new
                {
                    guild_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_special_roles", x => new { x.guild_id, x.type });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "special_emojis");

            migrationBuilder.DropTable(
                name: "special_roles");
        }
    }
}
