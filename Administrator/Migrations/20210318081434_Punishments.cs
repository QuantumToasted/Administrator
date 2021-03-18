using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Administrator.Migrations
{
    public partial class Punishments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "mute_role_id",
                table: "guilds",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.CreateTable(
                name: "punishments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    target_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    target_tag = table.Column<string>(type: "text", nullable: true),
                    moderator_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    moderator_tag = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    log_message_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    log_channel_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    attachment = table.Column<string>(type: "text", nullable: true),
                    punishment_type = table.Column<string>(type: "text", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoker_id = table.Column<ulong>(type: "numeric(20,0)", nullable: true),
                    revoker_tag = table.Column<string>(type: "text", nullable: true),
                    revocation_reason = table.Column<string>(type: "text", nullable: true),
                    appealed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    appeal_reason = table.Column<string>(type: "text", nullable: true),
                    expires = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    previous_channel_allow_value = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    previous_channel_deny_value = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    secondary_punishment_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_punishments", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "punishments");

            migrationBuilder.DropColumn(
                name: "mute_role_id",
                table: "guilds");
        }
    }
}
