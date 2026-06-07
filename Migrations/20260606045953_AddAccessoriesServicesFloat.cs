using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BraysTech.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessoriesServicesFloat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accessories",
                columns: table => new
                {
                    AccessoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Brand = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BuyingPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    LowStockAlert = table.Column<int>(type: "int", nullable: false),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    SupplierName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accessories", x => x.AccessoryID);
                    table.ForeignKey(
                        name: "FK_Accessories_Branches_BranchID",
                        column: x => x.BranchID,
                        principalTable: "Branches",
                        principalColumn: "BranchID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AccessorySales",
                columns: table => new
                {
                    SaleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StaffID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerPhone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalProfit = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    MpesaCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessorySales", x => x.SaleID);
                    table.ForeignKey(
                        name: "FK_AccessorySales_AspNetUsers_StaffID",
                        column: x => x.StaffID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessorySales_Branches_BranchID",
                        column: x => x.BranchID,
                        principalTable: "Branches",
                        principalColumn: "BranchID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MpesaFloats",
                columns: table => new
                {
                    FloatID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    TillNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TillName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentBalance = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    LowBalanceAlert = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MpesaFloats", x => x.FloatID);
                    table.ForeignKey(
                        name: "FK_MpesaFloats_Branches_BranchID",
                        column: x => x.BranchID,
                        principalTable: "Branches",
                        principalColumn: "BranchID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceRecords",
                columns: table => new
                {
                    RecordID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    StaffID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerPhone = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerIDNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChargeAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    MpesaCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldSimNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewSimNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneIMEI = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FaultDescription = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRecords", x => x.RecordID);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_AspNetUsers_StaffID",
                        column: x => x.StaffID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_Branches_BranchID",
                        column: x => x.BranchID,
                        principalTable: "Branches",
                        principalColumn: "BranchID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AccessorySaleItems",
                columns: table => new
                {
                    ItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SaleID = table.Column<int>(type: "int", nullable: false),
                    AccessoryID = table.Column<int>(type: "int", nullable: false),
                    AccessoryName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BuyingPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Profit = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessorySaleItems", x => x.ItemID);
                    table.ForeignKey(
                        name: "FK_AccessorySaleItems_Accessories_AccessoryID",
                        column: x => x.AccessoryID,
                        principalTable: "Accessories",
                        principalColumn: "AccessoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessorySaleItems_AccessorySales_SaleID",
                        column: x => x.SaleID,
                        principalTable: "AccessorySales",
                        principalColumn: "SaleID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FloatTransactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FloatID = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Reference = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StaffID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloatTransactions", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_FloatTransactions_AspNetUsers_StaffID",
                        column: x => x.StaffID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FloatTransactions_MpesaFloats_FloatID",
                        column: x => x.FloatID,
                        principalTable: "MpesaFloats",
                        principalColumn: "FloatID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_BranchID",
                table: "Accessories",
                column: "BranchID");

            migrationBuilder.CreateIndex(
                name: "IX_AccessorySaleItems_AccessoryID",
                table: "AccessorySaleItems",
                column: "AccessoryID");

            migrationBuilder.CreateIndex(
                name: "IX_AccessorySaleItems_SaleID",
                table: "AccessorySaleItems",
                column: "SaleID");

            migrationBuilder.CreateIndex(
                name: "IX_AccessorySales_BranchID",
                table: "AccessorySales",
                column: "BranchID");

            migrationBuilder.CreateIndex(
                name: "IX_AccessorySales_StaffID",
                table: "AccessorySales",
                column: "StaffID");

            migrationBuilder.CreateIndex(
                name: "IX_FloatTransactions_FloatID",
                table: "FloatTransactions",
                column: "FloatID");

            migrationBuilder.CreateIndex(
                name: "IX_FloatTransactions_StaffID",
                table: "FloatTransactions",
                column: "StaffID");

            migrationBuilder.CreateIndex(
                name: "IX_MpesaFloats_BranchID",
                table: "MpesaFloats",
                column: "BranchID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_BranchID",
                table: "ServiceRecords",
                column: "BranchID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_StaffID",
                table: "ServiceRecords",
                column: "StaffID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessorySaleItems");

            migrationBuilder.DropTable(
                name: "FloatTransactions");

            migrationBuilder.DropTable(
                name: "ServiceRecords");

            migrationBuilder.DropTable(
                name: "Accessories");

            migrationBuilder.DropTable(
                name: "AccessorySales");

            migrationBuilder.DropTable(
                name: "MpesaFloats");
        }
    }
}
