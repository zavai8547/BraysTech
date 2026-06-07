using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BraysTech.Migrations
{
    /// <inheritdoc />
    public partial class AddCashUpAndSimCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashUps",
                columns: table => new
                {
                    CashUpID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StaffID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    CashAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MpesaFloat = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ExpectedMpesa = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CashUpDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashUps", x => x.CashUpID);
                    table.ForeignKey(
                        name: "FK_CashUps_AspNetUsers_StaffID",
                        column: x => x.StaffID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashUps_Branches_BranchID",
                        column: x => x.BranchID,
                        principalTable: "Branches",
                        principalColumn: "BranchID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SimCards",
                columns: table => new
                {
                    SimCardID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Network = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    BuyingPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateSold = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SoldToName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SoldToPhone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsNewSim = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimCards", x => x.SimCardID);
                    table.ForeignKey(
                        name: "FK_SimCards_Branches_BranchID",
                        column: x => x.BranchID,
                        principalTable: "Branches",
                        principalColumn: "BranchID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CashUps_BranchID",
                table: "CashUps",
                column: "BranchID");

            migrationBuilder.CreateIndex(
                name: "IX_CashUps_StaffID",
                table: "CashUps",
                column: "StaffID");

            migrationBuilder.CreateIndex(
                name: "IX_SimCards_BranchID",
                table: "SimCards",
                column: "BranchID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashUps");

            migrationBuilder.DropTable(
                name: "SimCards");
        }
    }
}
