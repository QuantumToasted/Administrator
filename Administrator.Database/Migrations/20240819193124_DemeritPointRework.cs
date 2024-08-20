using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Administrator.Database.Migrations
{
    /// <inheritdoc />
    public partial class DemeritPointRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "demerit_points",
                table: "members");

            migrationBuilder.RenameColumn(
                name: "demerit_point_snapshot",
                table: "punishments",
                newName: "demerit_points_remaining");

            migrationBuilder.RenameColumn(
                name: "last_demerit_point_decay",
                table: "members",
                newName: "next_demerit_point_decay");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "demerit_points_remaining",
                table: "punishments",
                newName: "demerit_point_snapshot");

            migrationBuilder.RenameColumn(
                name: "next_demerit_point_decay",
                table: "members",
                newName: "last_demerit_point_decay");

            migrationBuilder.AddColumn<int>(
                name: "demerit_points",
                table: "members",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
