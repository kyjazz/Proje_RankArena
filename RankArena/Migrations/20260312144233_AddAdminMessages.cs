using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RankArena.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiverUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: true),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminMessages_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminMessages_TournamentId",
                table: "AdminMessages",
                column: "TournamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminMessages");
        }
    }
}
