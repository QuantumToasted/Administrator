using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Administrator.Database.Migrations
{
    /// <inheritdoc />
    public partial class TagLink_Text : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "label",
                table: "linked_tags",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "label",
                table: "linked_tags");
        }
    }
}
