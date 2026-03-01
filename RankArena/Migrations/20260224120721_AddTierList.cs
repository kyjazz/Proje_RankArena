using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RankArena.Migrations
{
    /// <inheritdoc />
    public partial class AddTierList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TierPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    TournamentItemId = table.Column<int>(type: "int", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TierPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TierPicks_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TierPicks_TournamentItems_TournamentItemId",
                        column: x => x.TournamentItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TierRunItems",
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
                    table.PrimaryKey("PK_TierRunItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TierRunItems_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TierRunItems_TournamentItems_TournamentItemId",
                        column: x => x.TournamentItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TierSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    TournamentItemId = table.Column<int>(type: "int", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    PlacedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TierSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TierSlots_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TierSlots_TournamentItems_TournamentItemId",
                        column: x => x.TournamentItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TierPicks_RunId",
                table: "TierPicks",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_TierPicks_TournamentItemId",
                table: "TierPicks",
                column: "TournamentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TierRunItems_RunId_Sequence",
                table: "TierRunItems",
                columns: new[] { "RunId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TierRunItems_RunId_TournamentItemId",
                table: "TierRunItems",
                columns: new[] { "RunId", "TournamentItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TierRunItems_TournamentItemId",
                table: "TierRunItems",
                column: "TournamentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TierSlots_RunId_TournamentItemId",
                table: "TierSlots",
                columns: new[] { "RunId", "TournamentItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TierSlots_TournamentItemId",
                table: "TierSlots",
                column: "TournamentItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TierPicks");

            migrationBuilder.DropTable(
                name: "TierRunItems");

            migrationBuilder.DropTable(
                name: "TierSlots");
        }
    }
}
