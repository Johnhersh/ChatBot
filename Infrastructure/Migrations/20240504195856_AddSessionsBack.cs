using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionsBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_ChatSessions_ActiveSessionId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_ActiveSessionId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ActiveSessionId",
                table: "Players");

            migrationBuilder.AddColumn<int>(
                name: "PlayerId",
                table: "ChatSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_PlayerId",
                table: "ChatSessions",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Players_PlayerId",
                table: "ChatSessions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_Players_PlayerId",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_PlayerId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "ChatSessions");

            migrationBuilder.AddColumn<int>(
                name: "ActiveSessionId",
                table: "Players",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_ActiveSessionId",
                table: "Players",
                column: "ActiveSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_ChatSessions_ActiveSessionId",
                table: "Players",
                column: "ActiveSessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id");
        }
    }
}
