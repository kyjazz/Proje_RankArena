using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RankArena.Migrations
{
    /// <inheritdoc />
    public partial class AddBracketTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BracketMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    MatchNumber = table.Column<int>(type: "int", nullable: false),
                    LeftItemId = table.Column<int>(type: "int", nullable: true),
                    RightItemId = table.Column<int>(type: "int", nullable: true),
                    WinnerItemId = table.Column<int>(type: "int", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BracketMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BracketMatches_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BracketMatches_TournamentItems_LeftItemId",
                        column: x => x.LeftItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BracketMatches_TournamentItems_RightItemId",
                        column: x => x.RightItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BracketMatches_TournamentItems_WinnerItemId",
                        column: x => x.WinnerItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BracketVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    SelectedItemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BracketVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BracketVotes_BracketMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "BracketMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BracketVotes_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BracketVotes_TournamentItems_SelectedItemId",
                        column: x => x.SelectedItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BracketMatches_LeftItemId",
                table: "BracketMatches",
                column: "LeftItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketMatches_RightItemId",
                table: "BracketMatches",
                column: "RightItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketMatches_RunId_Round_MatchNumber",
                table: "BracketMatches",
                columns: new[] { "RunId", "Round", "MatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BracketMatches_WinnerItemId",
                table: "BracketMatches",
                column: "WinnerItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketVotes_MatchId",
                table: "BracketVotes",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketVotes_RunId",
                table: "BracketVotes",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketVotes_SelectedItemId",
                table: "BracketVotes",
                column: "SelectedItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BracketVotes");

            migrationBuilder.DropTable(
                name: "BracketMatches");
        }
    }
}
