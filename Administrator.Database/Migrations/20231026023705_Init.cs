using System;
using Administrator.Core;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Administrator.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false),
                    filename = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "global_users",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    sent_initial_join_message = table.Column<bool>(type: "boolean", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: true),
                    highlights_snoozed_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    blacklisted_highlight_users = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false),
                    blacklisted_highlight_channels = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    last_xp_gain = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_level_up = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_global_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guild_users",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    blurb = table.Column<string>(type: "text", nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    last_xp_gain = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_level_up = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_users", x => new { x.guild, x.id });
                });

            migrationBuilder.CreateTable(
                name: "guilds",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    settings = table.Column<int>(type: "integer", nullable: false),
                    max_tags = table.Column<int>(type: "integer", nullable: true),
                    level_up_emoji = table.Column<string>(type: "text", nullable: false),
                    greeting = table.Column<JsonMessage>(type: "jsonb", nullable: true),
                    dm_greeting = table.Column<bool>(type: "boolean", nullable: false),
                    goodbye = table.Column<JsonMessage>(type: "jsonb", nullable: true),
                    was_visited = table.Column<bool>(type: "boolean", nullable: false),
                    punishment_text = table.Column<string>(type: "text", nullable: true),
                    xp_rate = table.Column<int>(type: "integer", nullable: true),
                    xp_interval = table.Column<TimeSpan>(type: "interval", nullable: true),
                    api_salt = table.Column<byte[]>(type: "bytea", nullable: true),
                    api_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    xp_exempt_channels = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false),
                    auto_quote_exempt_channels = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guilds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "punishments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    target = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    target_name = table.Column<string>(type: "text", nullable: false),
                    moderator = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    moderator_name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    log_channel = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    log_message = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    dm_channel = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    dm_message = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    attachment = table.Column<Guid>(type: "uuid", nullable: true),
                    message_prune_days = table.Column<int>(type: "integer", nullable: true),
                    expires = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoker = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    revoker_name = table.Column<string>(type: "text", nullable: true),
                    revocation_reason = table.Column<string>(type: "text", nullable: true),
                    appealed = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    appeal = table.Column<string>(type: "text", nullable: true),
                    appeal_status = table.Column<int>(type: "integer", nullable: true),
                    appeal_channel = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    appeal_message = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    channel = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    previous_allow_permissions = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    previous_deny_permissions = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    role = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    mode = table.Column<int>(type: "integer", nullable: true),
                    manually_revoked = table.Column<bool>(type: "boolean", nullable: true),
                    additional_punishment = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_punishments", x => x.id);
                    table.ForeignKey(
                        name: "fk_punishments_attachments_attachment",
                        column: x => x.attachment,
                        principalTable: "attachments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_punishments_punishments_additional_punishment",
                        column: x => x.additional_punishment,
                        principalTable: "punishments",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "highlights",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    author = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_highlights", x => x.id);
                    table.ForeignKey(
                        name: "fk_highlights_global_users_author",
                        column: x => x.author,
                        principalTable: "global_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text = table.Column<string>(type: "text", nullable: false),
                    author = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: true),
                    interval = table.Column<double>(type: "double precision", nullable: true),
                    created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_reminders_global_users_author",
                        column: x => x.author,
                        principalTable: "global_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "button_roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    message = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    emoji = table.Column<string>(type: "text", nullable: true),
                    text = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    exclusive_group = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_button_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_button_roles_guilds_guild",
                        column: x => x.guild,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emoji_stats",
                columns: table => new
                {
                    emoji = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    uses = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emoji_stats", x => x.emoji);
                    table.ForeignKey(
                        name: "fk_emoji_stats_guilds_guild",
                        column: x => x.guild,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "forum_auto_tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    regex = table.Column<bool>(type: "boolean", nullable: false),
                    tag = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_forum_auto_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_forum_auto_tags_guilds_guild",
                        column: x => x.guild,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invite_filter_exemptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    target = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    invite_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invite_filter_exemptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_invite_filter_exemptions_guilds_guild",
                        column: x => x.guild,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "level_rewards",
                columns: table => new
                {
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    granted_roles = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false),
                    revoked_roles = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_level_rewards", x => new { x.guild, x.tier, x.level });
                    table.ForeignKey(
                        name: "fk_level_rewards_guilds_guild",
                        column: x => x.guild,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logging_channels",
                columns: table => new
                {
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logging_channels", x => new { x.guild, x.type });
                    table.ForeignKey(
                        name: "fk_logging_channels_guilds_guild",
                        column: x => x.guild,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lua_commands",
                columns: table => new
                {
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<byte[]>(type: "bytea", nullable: false),
                    command = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lua_commands", x => new { x.guild, x.name });
                    table.ForeignKey(
                        name: "fk_lua_commands_guilds_guild",
                        column: x => x.guild,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    owner = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<JsonMessage>(type: "jsonb", nullable: true),
                    uses = table.Column<int>(type: "integer", nullable: false),
                    last_used = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attachment = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => new { x.guild, x.name });
                    table.ForeignKey(
                        name: "fk_tags_attachments_attachment",
                        column: x => x.attachment,
                        principalTable: "attachments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tags_guild_users_guild_owner",
                        columns: x => new { x.guild, x.owner },
                        principalTable: "guild_users",
                        principalColumns: new[] { "guild", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tags_guilds_guild",
                        column: x => x.guild,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "warning_punishments",
                columns: table => new
                {
                    guild = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    warnings = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warning_punishments", x => new { x.guild, x.warnings });
                    table.ForeignKey(
                        name: "fk_warning_punishments_guilds_guild",
                        column: x => x.guild,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_button_roles_guild",
                table: "button_roles",
                column: "guild");

            migrationBuilder.CreateIndex(
                name: "ix_emoji_stats_guild",
                table: "emoji_stats",
                column: "guild");

            migrationBuilder.CreateIndex(
                name: "ix_forum_auto_tags_guild",
                table: "forum_auto_tags",
                column: "guild");

            migrationBuilder.CreateIndex(
                name: "ix_forum_auto_tags_text",
                table: "forum_auto_tags",
                column: "text",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_highlights_author",
                table: "highlights",
                column: "author");

            migrationBuilder.CreateIndex(
                name: "ix_highlights_text",
                table: "highlights",
                column: "text");

            migrationBuilder.CreateIndex(
                name: "ix_invite_filter_exemptions_guild",
                table: "invite_filter_exemptions",
                column: "guild");

            migrationBuilder.CreateIndex(
                name: "ix_punishments_additional_punishment",
                table: "punishments",
                column: "additional_punishment");

            migrationBuilder.CreateIndex(
                name: "ix_punishments_attachment",
                table: "punishments",
                column: "attachment");

            migrationBuilder.CreateIndex(
                name: "ix_punishments_guild",
                table: "punishments",
                column: "guild");

            migrationBuilder.CreateIndex(
                name: "ix_punishments_target",
                table: "punishments",
                column: "target");

            migrationBuilder.CreateIndex(
                name: "ix_reminders_author",
                table: "reminders",
                column: "author");

            migrationBuilder.CreateIndex(
                name: "ix_reminders_expires",
                table: "reminders",
                column: "expires");

            migrationBuilder.CreateIndex(
                name: "ix_tags_attachment",
                table: "tags",
                column: "attachment");

            migrationBuilder.CreateIndex(
                name: "ix_tags_guild_owner",
                table: "tags",
                columns: new[] { "guild", "owner" });

            migrationBuilder.CreateIndex(
                name: "ix_tags_owner",
                table: "tags",
                column: "owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "button_roles");

            migrationBuilder.DropTable(
                name: "emoji_stats");

            migrationBuilder.DropTable(
                name: "forum_auto_tags");

            migrationBuilder.DropTable(
                name: "highlights");

            migrationBuilder.DropTable(
                name: "invite_filter_exemptions");

            migrationBuilder.DropTable(
                name: "level_rewards");

            migrationBuilder.DropTable(
                name: "logging_channels");

            migrationBuilder.DropTable(
                name: "lua_commands");

            migrationBuilder.DropTable(
                name: "punishments");

            migrationBuilder.DropTable(
                name: "reminders");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "warning_punishments");

            migrationBuilder.DropTable(
                name: "global_users");

            migrationBuilder.DropTable(
                name: "attachments");

            migrationBuilder.DropTable(
                name: "guild_users");

            migrationBuilder.DropTable(
                name: "guilds");
        }
    }
}
