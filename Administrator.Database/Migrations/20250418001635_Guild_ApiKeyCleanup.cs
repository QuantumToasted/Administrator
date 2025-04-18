using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Administrator.Database.Migrations
{
    /// <inheritdoc />
    public partial class Guild_ApiKeyCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "api_key_hash",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "api_key_salt",
                table: "guilds");

            migrationBuilder.AddColumn<string>(
                name: "api_key",
                table: "guilds",
                type: "text",
                nullable: false,
                defaultValueSql: "REPLACE(uuid_generate_v4()::text, '-', '' )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "api_key",
                table: "guilds");

            migrationBuilder.AddColumn<byte[]>(
                name: "api_key_hash",
                table: "guilds",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "api_key_salt",
                table: "guilds",
                type: "bytea",
                nullable: true);
        }
    }
}
