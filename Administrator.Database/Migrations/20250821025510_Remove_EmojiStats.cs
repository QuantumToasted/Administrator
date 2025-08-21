using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Administrator.Database.Migrations
{
    /// <inheritdoc />
    public partial class Remove_EmojiStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emoji_stats");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emoji_stats",
                columns: table => new
                {
                    emoji_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    uses = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emoji_stats", x => x.emoji_id);
                    table.ForeignKey(
                        name: "fk_emoji_stats_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_emoji_stats_guild_id",
                table: "emoji_stats",
                column: "guild_id");
        }
    }
}
