using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActiveSessionTwoWay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Players_ActiveSessionId",
                table: "Players",
                column: "ActiveSessionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_ChatSessions_ActiveSessionId",
                table: "Players",
                column: "ActiveSessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_ChatSessions_ActiveSessionId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_ActiveSessionId",
                table: "Players");
        }
    }
}
