using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BraysTech.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    TransferID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FromBranchID = table.Column<int>(type: "int", nullable: false),
                    ToBranchID = table.Column<int>(type: "int", nullable: false),
                    InitiatedByID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceivedByID = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CancellationReason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.TransferID);
                    table.ForeignKey(
                        name: "FK_StockTransfers_AspNetUsers_InitiatedByID",
                        column: x => x.InitiatedByID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransfers_AspNetUsers_ReceivedByID",
                        column: x => x.ReceivedByID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_Branches_FromBranchID",
                        column: x => x.FromBranchID,
                        principalTable: "Branches",
                        principalColumn: "BranchID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Branches_ToBranchID",
                        column: x => x.ToBranchID,
                        principalTable: "Branches",
                        principalColumn: "BranchID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StockTransferItems",
                columns: table => new
                {
                    TransferItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TransferID = table.Column<int>(type: "int", nullable: false),
                    StockID = table.Column<int>(type: "int", nullable: false),
                    IMEI = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferItems", x => x.TransferItemID);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_IMEIStock_StockID",
                        column: x => x.StockID,
                        principalTable: "IMEIStock",
                        principalColumn: "StockID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_StockTransfers_TransferID",
                        column: x => x.TransferID,
                        principalTable: "StockTransfers",
                        principalColumn: "TransferID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_StockID",
                table: "StockTransferItems",
                column: "StockID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_TransferID",
                table: "StockTransferItems",
                column: "TransferID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromBranchID",
                table: "StockTransfers",
                column: "FromBranchID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_InitiatedByID",
                table: "StockTransfers",
                column: "InitiatedByID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ReceivedByID",
                table: "StockTransfers",
                column: "ReceivedByID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToBranchID",
                table: "StockTransfers",
                column: "ToBranchID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTransferItems");

            migrationBuilder.DropTable(
                name: "StockTransfers");
        }
    }
}
