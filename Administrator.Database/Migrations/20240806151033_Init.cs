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
                name: "attachment",
                columns: table => new
                {
                    key = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachment", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "guilds",
                columns: table => new
                {
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    settings = table.Column<int>(type: "integer", nullable: false),
                    maximum_tags_per_user = table.Column<int>(type: "integer", nullable: true),
                    level_up_emoji = table.Column<string>(type: "text", nullable: false),
                    greeting_message = table.Column<JsonMessage>(type: "jsonb", nullable: true),
                    dm_greeting_message = table.Column<bool>(type: "boolean", nullable: false),
                    goodbye_message = table.Column<JsonMessage>(type: "jsonb", nullable: true),
                    was_visited = table.Column<bool>(type: "boolean", nullable: false),
                    custom_punishment_text = table.Column<string>(type: "text", nullable: true),
                    custom_xp_rate = table.Column<int>(type: "integer", nullable: true),
                    custom_xp_interval = table.Column<TimeSpan>(type: "interval", nullable: true),
                    api_key_salt = table.Column<byte[]>(type: "bytea", nullable: true),
                    api_key_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    xp_exempt_channel_ids = table.Column<long[]>(type: "bigint[]", nullable: false),
                    auto_quote_exempt_channel_ids = table.Column<long[]>(type: "bigint[]", nullable: false),
                    default_ban_prune_days = table.Column<int>(type: "integer", nullable: false),
                    default_warning_demerit_points = table.Column<int>(type: "integer", nullable: false),
                    demerit_points_decay_interval = table.Column<TimeSpan>(type: "interval", nullable: true),
                    join_role_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guilds", x => x.guild_id);
                });

            migrationBuilder.CreateTable(
                name: "linked_tags",
                columns: table => new
                {
                    from = table.Column<string>(type: "text", nullable: false),
                    to = table.Column<string>(type: "text", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    style = table.Column<byte>(type: "smallint", nullable: false),
                    is_ephemeral = table.Column<bool>(type: "boolean", nullable: false),
                    id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_linked_tags", x => new { x.from, x.to });
                });

            migrationBuilder.CreateTable(
                name: "members",
                columns: table => new
                {
                    user = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    blurb = table.Column<string>(type: "text", nullable: false),
                    last_demerit_point_decay = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    demerit_points = table.Column<int>(type: "integer", nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    last_xp_gain = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_level_up = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => new { x.guild_id, x.user });
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user = table.Column<long>(type: "bigint", nullable: false),
                    was_sent_initial_join_message = table.Column<bool>(type: "boolean", nullable: false),
                    time_zone = table.Column<string>(type: "text", nullable: true),
                    highlights_snoozed_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    blacklisted_highlight_user_ids = table.Column<long[]>(type: "bigint[]", nullable: false),
                    blacklisted_highlight_channel_ids = table.Column<long[]>(type: "bigint[]", nullable: false),
                    resume_highlights_after_message_count = table.Column<int>(type: "integer", nullable: false),
                    resume_highlights_after_interval = table.Column<TimeSpan>(type: "interval", nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    last_xp_gain = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_level_up = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.user);
                });

            migrationBuilder.CreateTable(
                name: "auto_tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    is_regex = table.Column<bool>(type: "boolean", nullable: false),
                    tag_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auto_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_auto_tags_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                });

            migrationBuilder.CreateTable(
                name: "automatic_punishments",
                columns: table => new
                {
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    demerit_points = table.Column<int>(type: "integer", nullable: false),
                    punishment_type = table.Column<int>(type: "integer", nullable: false),
                    punishment_duration = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automatic_punishments", x => new { x.guild_id, x.demerit_points });
                    table.ForeignKey(
                        name: "fk_automatic_punishments_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                });

            migrationBuilder.CreateTable(
                name: "button_roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    channel_id = table.Column<long>(type: "bigint", nullable: false),
                    message_id = table.Column<long>(type: "bigint", nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    emoji = table.Column<string>(type: "text", nullable: true),
                    text = table.Column<string>(type: "text", nullable: true),
                    style = table.Column<byte>(type: "smallint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    exclusive_group_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_button_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_button_roles_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                });

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

            migrationBuilder.CreateTable(
                name: "invite_filter_exemptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    exemption_type = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<long>(type: "bigint", nullable: true),
                    invite_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invite_filter_exemptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_invite_filter_exemptions_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                });

            migrationBuilder.CreateTable(
                name: "level_rewards",
                columns: table => new
                {
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    granted_role_ids = table.Column<long[]>(type: "bigint[]", nullable: false),
                    revoked_role_ids = table.Column<long[]>(type: "bigint[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_level_rewards", x => new { x.guild_id, x.tier, x.level });
                    table.ForeignKey(
                        name: "fk_level_rewards_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                });

            migrationBuilder.CreateTable(
                name: "logging_channels",
                columns: table => new
                {
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type = table.Column<int>(type: "integer", nullable: false),
                    channel_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logging_channels", x => new { x.guild_id, x.event_type });
                    table.ForeignKey(
                        name: "fk_logging_channels_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                });

            migrationBuilder.CreateTable(
                name: "lua_commands",
                columns: table => new
                {
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<byte[]>(type: "bytea", nullable: false),
                    command = table.Column<byte[]>(type: "bytea", nullable: false),
                    persistence = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lua_commands", x => new { x.guild_id, x.name });
                    table.ForeignKey(
                        name: "fk_lua_commands_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                });

            migrationBuilder.CreateTable(
                name: "punishments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    target = table.Column<UserSnapshot>(type: "jsonb", nullable: false),
                    moderator = table.Column<UserSnapshot>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    log_channel_id = table.Column<long>(type: "bigint", nullable: true),
                    log_message_id = table.Column<long>(type: "bigint", nullable: true),
                    dm_channel_id = table.Column<long>(type: "bigint", nullable: true),
                    dm_message_id = table.Column<long>(type: "bigint", nullable: true),
                    attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoker = table.Column<UserSnapshot>(type: "jsonb", nullable: true),
                    revocation_reason = table.Column<string>(type: "text", nullable: true),
                    appealed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    appeal_text = table.Column<string>(type: "text", nullable: true),
                    appeal_status = table.Column<int>(type: "integer", nullable: true),
                    appeal_channel_id = table.Column<long>(type: "bigint", nullable: true),
                    appeal_message_id = table.Column<long>(type: "bigint", nullable: true),
                    message_prune_days = table.Column<int>(type: "integer", nullable: true),
                    ban_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    channel_id = table.Column<long>(type: "bigint", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    previous_channel_allow_permissions = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    previous_channel_deny_permissions = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    role_id = table.Column<long>(type: "bigint", nullable: true),
                    mode = table.Column<int>(type: "integer", nullable: true),
                    timed_role_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    timeout_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    was_manually_revoked = table.Column<bool>(type: "boolean", nullable: true),
                    demerit_points = table.Column<int>(type: "integer", nullable: true),
                    demerit_point_snapshot = table.Column<int>(type: "integer", nullable: true),
                    additional_punishment_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_punishments", x => x.id);
                    table.ForeignKey(
                        name: "fk_punishments_attachment_attachment_id",
                        column: x => x.attachment_id,
                        principalTable: "attachment",
                        principalColumn: "key");
                    table.ForeignKey(
                        name: "fk_punishments_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                    table.ForeignKey(
                        name: "fk_punishments_punishments_additional_punishment_id",
                        column: x => x.additional_punishment_id,
                        principalTable: "punishments",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    aliases = table.Column<string[]>(type: "text[]", nullable: false),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<JsonMessage>(type: "jsonb", nullable: true),
                    uses = table.Column<int>(type: "integer", nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attachment_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => new { x.guild_id, x.name });
                    table.ForeignKey(
                        name: "fk_tags_attachment_attachment_id",
                        column: x => x.attachment_id,
                        principalTable: "attachment",
                        principalColumn: "key");
                    table.ForeignKey(
                        name: "fk_tags_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "guild_id");
                    table.ForeignKey(
                        name: "fk_tags_members_guild_id_owner_id",
                        columns: x => new { x.guild_id, x.owner_id },
                        principalTable: "members",
                        principalColumns: new[] { "guild_id", "user" });
                });

            migrationBuilder.CreateTable(
                name: "highlights",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: true),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_highlights", x => x.id);
                    table.ForeignKey(
                        name: "fk_highlights_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "user");
                });

            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text = table.Column<string>(type: "text", nullable: false),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    channel_id = table.Column<long>(type: "bigint", nullable: false),
                    repeat_mode = table.Column<int>(type: "integer", nullable: true),
                    repeat_interval = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_reminders_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "user");
                });

            migrationBuilder.CreateIndex(
                name: "ix_auto_tags_channel_id",
                table: "auto_tags",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_auto_tags_guild_id",
                table: "auto_tags",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_button_roles_guild_id",
                table: "button_roles",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_emoji_stats_guild_id",
                table: "emoji_stats",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_highlights_author_id",
                table: "highlights",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_invite_filter_exemptions_guild_id",
                table: "invite_filter_exemptions",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_linked_tags_from",
                table: "linked_tags",
                column: "from");

            migrationBuilder.CreateIndex(
                name: "ix_punishments_additional_punishment_id",
                table: "punishments",
                column: "additional_punishment_id");

            migrationBuilder.CreateIndex(
                name: "ix_punishments_attachment_id",
                table: "punishments",
                column: "attachment_id");

            migrationBuilder.CreateIndex(
                name: "ix_punishments_guild_id",
                table: "punishments",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_reminders_author_id",
                table: "reminders",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_reminders_expires_at",
                table: "reminders",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_tags_aliases",
                table: "tags",
                column: "aliases");

            migrationBuilder.CreateIndex(
                name: "ix_tags_attachment_id",
                table: "tags",
                column: "attachment_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_guild_id_owner_id",
                table: "tags",
                columns: new[] { "guild_id", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tags_owner_id",
                table: "tags",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auto_tags");

            migrationBuilder.DropTable(
                name: "automatic_punishments");

            migrationBuilder.DropTable(
                name: "button_roles");

            migrationBuilder.DropTable(
                name: "emoji_stats");

            migrationBuilder.DropTable(
                name: "highlights");

            migrationBuilder.DropTable(
                name: "invite_filter_exemptions");

            migrationBuilder.DropTable(
                name: "level_rewards");

            migrationBuilder.DropTable(
                name: "linked_tags");

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
                name: "users");

            migrationBuilder.DropTable(
                name: "attachment");

            migrationBuilder.DropTable(
                name: "guilds");

            migrationBuilder.DropTable(
                name: "members");
        }
    }
}
