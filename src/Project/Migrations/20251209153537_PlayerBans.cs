using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuringMachinesAPI.Migrations
{
    /// <inheritdoc />
    public partial class PlayerBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BannedUntil",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanReason",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "BannedUntil",
                table: "Players");
        }
    }
}
