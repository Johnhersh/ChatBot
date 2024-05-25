using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RequiredSessionCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_Characters_CharacterId",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_CharacterId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "ChatSessions");

            migrationBuilder.AddColumn<int>(
                name: "ChatSessionId",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ChatSessionId",
                table: "Characters",
                column: "ChatSessionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_ChatSessions_ChatSessionId",
                table: "Characters",
                column: "ChatSessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_ChatSessions_ChatSessionId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_ChatSessionId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ChatSessionId",
                table: "Characters");

            migrationBuilder.AddColumn<int>(
                name: "CharacterId",
                table: "ChatSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CharacterId",
                table: "ChatSessions",
                column: "CharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Characters_CharacterId",
                table: "ChatSessions",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
