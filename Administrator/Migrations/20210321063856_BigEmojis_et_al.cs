using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class BigEmojis_et_al : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mute_role_id",
                table: "guilds");

            migrationBuilder.AddColumn<List<string>>(
                name: "prefixes",
                table: "guilds",
                type: "text[]",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "big_emojis",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    guild_id = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    is_animated = table.Column<bool>(type: "boolean", nullable: false),
                    emoji_type = table.Column<string>(type: "text", nullable: false),
                    approver_id = table.Column<ulong>(type: "numeric(20,0)", nullable: true),
                    approver_tag = table.Column<string>(type: "text", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    denier_id = table.Column<ulong>(type: "numeric(20,0)", nullable: true),
                    denier_tag = table.Column<string>(type: "text", nullable: true),
                    denied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    requester_id = table.Column<ulong>(type: "numeric(20,0)", nullable: true),
                    requester_tag = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_big_emojis", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "big_emojis");

            migrationBuilder.DropColumn(
                name: "prefixes",
                table: "guilds");

            migrationBuilder.AddColumn<decimal>(
                name: "mute_role_id",
                table: "guilds",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
