using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Administrator.Database.Migrations
{
    /// <inheritdoc />
    public partial class Reminder_IntervalTypeChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.AlterColumn<int>(
                name: "repeat_interval",
                table: "reminders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "api_key",
                table: "guilds",
                type: "text",
                nullable: false,
                defaultValueSql: "REPLACE(gen_random_uuid()::text, '-', '' )",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValueSql: "REPLACE(uuid_generate_v4()::text, '-', '' )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.AlterColumn<double>(
                name: "repeat_interval",
                table: "reminders",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "api_key",
                table: "guilds",
                type: "text",
                nullable: false,
                defaultValueSql: "REPLACE(uuid_generate_v4()::text, '-', '' )",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValueSql: "REPLACE(gen_random_uuid()::text, '-', '' )");
        }
    }
}
