using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActivePlayerSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_Players_PlayerId",
                table: "ChatSessions");

            migrationBuilder.AlterColumn<long>(
                name: "TelegramId",
                table: "Players",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "ActiveSessionId",
                table: "Players",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlayerId",
                table: "ChatSessions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ActiveSessionId",
                table: "Players",
                column: "ActiveSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Players_PlayerId",
                table: "ChatSessions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");

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
                name: "FK_ChatSessions_Players_PlayerId",
                table: "ChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_ChatSessions_ActiveSessionId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_ActiveSessionId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ActiveSessionId",
                table: "Players");

            migrationBuilder.AlterColumn<long>(
                name: "TelegramId",
                table: "Players",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlayerId",
                table: "ChatSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Players_PlayerId",
                table: "ChatSessions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
