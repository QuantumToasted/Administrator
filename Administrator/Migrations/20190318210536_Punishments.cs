using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Administrator.Migrations
{
    public partial class Punishments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Punishments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    GuildId = table.Column<decimal>(nullable: false),
                    TargetId = table.Column<decimal>(nullable: false),
                    ModeratorId = table.Column<decimal>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    IsAppealable = table.Column<bool>(nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(nullable: true),
                    RevokerId = table.Column<decimal>(nullable: true),
                    RevocationReason = table.Column<string>(nullable: true),
                    AppealedAt = table.Column<DateTimeOffset>(nullable: true),
                    AppealReason = table.Column<string>(nullable: true),
                    Duration = table.Column<TimeSpan>(nullable: true),
                    TemporaryBan_Duration = table.Column<TimeSpan>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Punishments", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Punishments");
        }
    }
}
