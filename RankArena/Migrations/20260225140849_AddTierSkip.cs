using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RankArena.Migrations
{
    /// <inheritdoc />
    public partial class AddTierSkip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TierSkips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    TournamentItemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TierSkips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TierSkips_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TierSkips_TournamentItems_TournamentItemId",
                        column: x => x.TournamentItemId,
                        principalTable: "TournamentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TierSkips_RunId_TournamentItemId",
                table: "TierSkips",
                columns: new[] { "RunId", "TournamentItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TierSkips_TournamentItemId",
                table: "TierSkips",
                column: "TournamentItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TierSkips");
        }
    }
}
