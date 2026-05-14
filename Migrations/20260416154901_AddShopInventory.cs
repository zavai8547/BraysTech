using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SplashCityCarwash.Migrations
{
    /// <inheritdoc />
    public partial class AddShopInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 4);

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BuyingPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    LowStockAlert = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopSales",
                columns: table => new
                {
                    SaleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StaffID = table.Column<string>(type: "varchar(255)", nullable: false)
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
                    table.PrimaryKey("PK_ShopSales", x => x.SaleID);
                    table.ForeignKey(
                        name: "FK_ShopSales_AspNetUsers_StaffID",
                        column: x => x.StaffID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    MovementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByID = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.MovementID);
                    table.ForeignKey(
                        name: "FK_StockMovements_AspNetUsers_CreatedByID",
                        column: x => x.CreatedByID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopSaleItems",
                columns: table => new
                {
                    ItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SaleID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BuyingPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Profit = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopSaleItems", x => x.ItemID);
                    table.ForeignKey(
                        name: "FK_ShopSaleItems_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopSaleItems_ShopSales_SaleID",
                        column: x => x.SaleID,
                        principalTable: "ShopSales",
                        principalColumn: "SaleID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ShopSaleItems_ProductID",
                table: "ShopSaleItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ShopSaleItems_SaleID",
                table: "ShopSaleItems",
                column: "SaleID");

            migrationBuilder.CreateIndex(
                name: "IX_ShopSales_StaffID",
                table: "ShopSales",
                column: "StaffID");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedByID",
                table: "StockMovements",
                column: "CreatedByID");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductID",
                table: "StockMovements",
                column: "ProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopSaleItems");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "ShopSales");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.InsertData(
                table: "ServicePackages",
                columns: new[] { "ServiceID", "CreatedAt", "Description", "EstimatedDuration", "IsActive", "Price", "ServiceName" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6151), "Exterior wash sedan", 15, true, 300m, "Normal Wash - Sedan" },
                    { 2, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6159), "Exterior wash SUV", 20, true, 400m, "Normal Wash - SUV" },
                    { 3, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6161), "Exterior wash van", 20, true, 500m, "Normal Wash - Van" },
                    { 4, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6163), "Exterior wash tour van", 25, true, 700m, "Normal Wash - Tour Van" },
                    { 5, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6165), "Full interior and exterior", 45, true, 1000m, "Body & Interior + Vacuum" },
                    { 6, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6166), "Premium package", 90, true, 3500m, "Body & Interior + Engine + Polish" },
                    { 7, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6168), "All inclusive detailing", 120, true, 5000m, "Full Detailing" },
                    { 8, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6170), "Watermark removal", 60, true, 2000m, "Removing Watermarks" },
                    { 9, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6172), "Rim cleaning and restoration", 60, true, 3000m, "Rims Restoration" }
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "SettingID", "SettingKey", "SettingValue", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "business_name", "Kujaributuu Carwash & Detailing", new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(5926) },
                    { 2, "currency", "KES", new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(5942) },
                    { 3, "vat_rate", "16", new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(5944) },
                    { 4, "receipt_footer", "Thank you for choosing Kujaributuu!", new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(5945) }
                });
        }
    }
}
