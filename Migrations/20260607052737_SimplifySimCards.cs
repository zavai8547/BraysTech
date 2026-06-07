using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BraysTech.Migrations
{
    /// <inheritdoc />
    public partial class SimplifySimCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsNewSim",
                table: "SimCards",
                newName: "IsReplacement");

            migrationBuilder.AddColumn<string>(
                name: "CustomerIDNumber",
                table: "SimCards",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MpesaCode",
                table: "SimCards",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NewSimNumber",
                table: "SimCards",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OldSimNumber",
                table: "SimCards",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "SimCards",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerIDNumber",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "MpesaCode",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "NewSimNumber",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "OldSimNumber",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "SimCards");

            migrationBuilder.RenameColumn(
                name: "IsReplacement",
                table: "SimCards",
                newName: "IsNewSim");
        }
    }
}
