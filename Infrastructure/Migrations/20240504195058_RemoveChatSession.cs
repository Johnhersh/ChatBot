using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChatSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayerId",
                table: "ChatSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_PlayerId",
                table: "ChatSessions",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Players_PlayerId",
                table: "ChatSessions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }
    }
}
