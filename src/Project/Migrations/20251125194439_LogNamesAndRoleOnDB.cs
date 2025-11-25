using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuringMachinesAPI.Migrations
{
    /// <inheritdoc />
    public partial class LogNamesAndRoleOnDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminLogs_Players_ActorId",
                table: "AdminLogs");

            migrationBuilder.AlterColumn<int>(
                name: "TargetEntityId",
                table: "AdminLogs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ActorId",
                table: "AdminLogs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "AdminLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActorRole",
                table: "AdminLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetEntityName",
                table: "AdminLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminLogs_Players_ActorId",
                table: "AdminLogs",
                column: "ActorId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminLogs_Players_ActorId",
                table: "AdminLogs");

            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "AdminLogs");

            migrationBuilder.DropColumn(
                name: "ActorRole",
                table: "AdminLogs");

            migrationBuilder.DropColumn(
                name: "TargetEntityName",
                table: "AdminLogs");

            migrationBuilder.AlterColumn<int>(
                name: "TargetEntityId",
                table: "AdminLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActorId",
                table: "AdminLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminLogs_Players_ActorId",
                table: "AdminLogs",
                column: "ActorId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
