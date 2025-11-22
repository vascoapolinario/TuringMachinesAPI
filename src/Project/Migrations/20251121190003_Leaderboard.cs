using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TuringMachinesAPI.Migrations
{
    /// <inheritdoc />
    public partial class Leaderboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaderboardLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkshopItemId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaderboardLevels_WorkshopItems_WorkshopItemId",
                        column: x => x.WorkshopItemId,
                        principalTable: "WorkshopItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LevelSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    LeaderboardLevelId = table.Column<int>(type: "integer", nullable: false),
                    Time = table.Column<double>(type: "double precision", nullable: false),
                    NodeCount = table.Column<int>(type: "integer", nullable: false),
                    ConnectionCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LevelSubmissions_LeaderboardLevels_LeaderboardLevelId",
                        column: x => x.LeaderboardLevelId,
                        principalTable: "LeaderboardLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LevelSubmissions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardLevels_WorkshopItemId",
                table: "LeaderboardLevels",
                column: "WorkshopItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LevelSubmissions_LeaderboardLevelId",
                table: "LevelSubmissions",
                column: "LeaderboardLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_LevelSubmissions_PlayerId",
                table: "LevelSubmissions",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LevelSubmissions");

            migrationBuilder.DropTable(
                name: "LeaderboardLevels");
        }
    }
}
