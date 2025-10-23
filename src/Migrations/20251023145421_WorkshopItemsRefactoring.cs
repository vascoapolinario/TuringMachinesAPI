using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TuringMachinesAPI.Migrations
{
    /// <inheritdoc />
    public partial class WorkshopItemsRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Levels");

            migrationBuilder.DropTable(
                name: "Machines");

            migrationBuilder.CreateTable(
                name: "LevelWorkshopItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkshopItemId = table.Column<int>(type: "integer", nullable: false),
                    LevelType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DetailedDescription = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    AlphabetJson = table.Column<string>(type: "text", nullable: false),
                    TransformTestsJson = table.Column<string>(type: "text", nullable: true),
                    CorrectExamplesJson = table.Column<string>(type: "text", nullable: true),
                    WrongExamplesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelWorkshopItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MachineWorkshopItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkshopItemId = table.Column<int>(type: "integer", nullable: false),
                    AlphabetJson = table.Column<string>(type: "text", nullable: false),
                    NodesJson = table.Column<string>(type: "text", nullable: false),
                    ConnectionsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineWorkshopItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LevelWorkshopItems");

            migrationBuilder.DropTable(
                name: "MachineWorkshopItems");

            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LevelData = table.Column<string>(type: "text", nullable: false),
                    LevelType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkshopItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Machines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MachineData = table.Column<string>(type: "text", nullable: false),
                    WorkshopItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.Id);
                });
        }
    }
}
