using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RankArena.Migrations
{
    /// <inheritdoc />
    public partial class AddBlindRankTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlindPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    TournamentItemId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlindPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlindPicks_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BlindPicks_TournamentItems_TournamentItemId",
                        column: x => x.TournamentItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BlindRunItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    TournamentItemId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlindRunItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlindRunItems_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlindRunItems_TournamentItems_TournamentItemId",
                        column: x => x.TournamentItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BlindSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    TournamentItemId = table.Column<int>(type: "int", nullable: true),
                    FilledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlindSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlindSlots_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlindSlots_TournamentItems_TournamentItemId",
                        column: x => x.TournamentItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlindPicks_RunId",
                table: "BlindPicks",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_BlindPicks_TournamentItemId",
                table: "BlindPicks",
                column: "TournamentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BlindRunItems_RunId_Sequence",
                table: "BlindRunItems",
                columns: new[] { "RunId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlindRunItems_RunId_TournamentItemId",
                table: "BlindRunItems",
                columns: new[] { "RunId", "TournamentItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlindRunItems_TournamentItemId",
                table: "BlindRunItems",
                column: "TournamentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BlindSlots_RunId_Position",
                table: "BlindSlots",
                columns: new[] { "RunId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlindSlots_TournamentItemId",
                table: "BlindSlots",
                column: "TournamentItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlindPicks");

            migrationBuilder.DropTable(
                name: "BlindRunItems");

            migrationBuilder.DropTable(
                name: "BlindSlots");
        }
    }
}
