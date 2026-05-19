using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BraysTech.Migrations
{
    /// <inheritdoc />
    public partial class AddFaultTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateMarkedFaulty",
                table: "IMEIStock",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaultReason",
                table: "IMEIStock",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RepairStatus",
                table: "IMEIStock",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TechnicianNotes",
                table: "IMEIStock",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "WarrantyClaim",
                table: "IMEIStock",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateMarkedFaulty",
                table: "IMEIStock");

            migrationBuilder.DropColumn(
                name: "FaultReason",
                table: "IMEIStock");

            migrationBuilder.DropColumn(
                name: "RepairStatus",
                table: "IMEIStock");

            migrationBuilder.DropColumn(
                name: "TechnicianNotes",
                table: "IMEIStock");

            migrationBuilder.DropColumn(
                name: "WarrantyClaim",
                table: "IMEIStock");
        }
    }
}
