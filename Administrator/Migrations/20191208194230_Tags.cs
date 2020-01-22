using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class Tags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    AuthorId = table.Column<decimal>(nullable: false),
                    Response = table.Column<string>(nullable: true),
                    Image = table.Column<byte[]>(nullable: true),
                    Format = table.Column<byte>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Uses = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => new { x.GuildId, x.Name });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
