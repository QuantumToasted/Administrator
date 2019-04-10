using Microsoft.EntityFrameworkCore.Migrations;

namespace Administrator.Migrations
{
    public partial class ModmailTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModmailMessage_Modmail_SourceId",
                table: "ModmailMessage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModmailMessage",
                table: "ModmailMessage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Modmail",
                table: "Modmail");

            migrationBuilder.RenameTable(
                name: "ModmailMessage",
                newName: "ModmailMessages");

            migrationBuilder.RenameTable(
                name: "Modmail",
                newName: "Modmails");

            migrationBuilder.RenameIndex(
                name: "IX_ModmailMessage_SourceId",
                table: "ModmailMessages",
                newName: "IX_ModmailMessages_SourceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModmailMessages",
                table: "ModmailMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Modmails",
                table: "Modmails",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModmailMessages_Modmails_SourceId",
                table: "ModmailMessages",
                column: "SourceId",
                principalTable: "Modmails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModmailMessages_Modmails_SourceId",
                table: "ModmailMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Modmails",
                table: "Modmails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModmailMessages",
                table: "ModmailMessages");

            migrationBuilder.RenameTable(
                name: "Modmails",
                newName: "Modmail");

            migrationBuilder.RenameTable(
                name: "ModmailMessages",
                newName: "ModmailMessage");

            migrationBuilder.RenameIndex(
                name: "IX_ModmailMessages_SourceId",
                table: "ModmailMessage",
                newName: "IX_ModmailMessage_SourceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Modmail",
                table: "Modmail",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModmailMessage",
                table: "ModmailMessage",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModmailMessage_Modmail_SourceId",
                table: "ModmailMessage",
                column: "SourceId",
                principalTable: "Modmail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
