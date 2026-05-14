using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SplashCityCarwash.Migrations
{
    /// <inheritdoc />
    public partial class AddWashersMpesaPaidStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "MpesaCode",
                table: "Transactions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Transactions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransactionWashers",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TransactionID = table.Column<int>(type: "int", nullable: false),
                    WasherID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionWashers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TransactionWashers_AspNetUsers_WasherID",
                        column: x => x.WasherID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionWashers_Transactions_TransactionID",
                        column: x => x.TransactionID,
                        principalTable: "Transactions",
                        principalColumn: "TransactionID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Description", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6151), "Exterior wash sedan", 300m, "Normal Wash - Sedan" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Description", "EstimatedDuration", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6159), "Exterior wash SUV", 20, 400m, "Normal Wash - SUV" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Description", "EstimatedDuration", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6161), "Exterior wash van", 20, 500m, "Normal Wash - Van" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 4,
                columns: new[] { "CreatedAt", "Description", "EstimatedDuration", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6163), "Exterior wash tour van", 25, 700m, "Normal Wash - Tour Van" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 5,
                columns: new[] { "CreatedAt", "Description", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6165), "Full interior and exterior", 1000m, "Body & Interior + Vacuum" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 6,
                columns: new[] { "CreatedAt", "Description", "EstimatedDuration", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6166), "Premium package", 90, 3500m, "Body & Interior + Engine + Polish" });

            migrationBuilder.InsertData(
                table: "ServicePackages",
                columns: new[] { "ServiceID", "CreatedAt", "Description", "EstimatedDuration", "IsActive", "Price", "ServiceName" },
                values: new object[,]
                {
                    { 7, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6168), "All inclusive detailing", 120, true, 5000m, "Full Detailing" },
                    { 8, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6170), "Watermark removal", 60, true, 2000m, "Removing Watermarks" },
                    { 9, new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(6172), "Rim cleaning and restoration", 60, true, 3000m, "Rims Restoration" }
                });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 1,
                columns: new[] { "SettingValue", "UpdatedAt" },
                values: new object[] { "Kujaributuu Carwash & Detailing", new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(5926) });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(5942));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(5944));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 4,
                columns: new[] { "SettingValue", "UpdatedAt" },
                values: new object[] { "Thank you for choosing Kujaributuu!", new DateTime(2026, 4, 15, 16, 20, 16, 440, DateTimeKind.Local).AddTicks(5945) });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionWashers_TransactionID",
                table: "TransactionWashers",
                column: "TransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionWashers_WasherID",
                table: "TransactionWashers",
                column: "WasherID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionWashers");

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

            migrationBuilder.DropColumn(
                name: "MpesaCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "CarModel",
                table: "Vehicles",
                newName: "Model");

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Description", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8762), "Exterior rinse and dry", 500m, "Basic Wash" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Description", "EstimatedDuration", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8771), "Exterior + windows + tire shine", 25, 800m, "Standard Wash" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Description", "EstimatedDuration", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8774), "Full interior vacuum and wipe", 30, 1000m, "Interior Cleaning" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 4,
                columns: new[] { "CreatedAt", "Description", "EstimatedDuration", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8777), "Interior + exterior full detail", 90, 2500m, "Full Detail" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 5,
                columns: new[] { "CreatedAt", "Description", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8779), "Engine bay cleaning", 1500m, "Engine Wash" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "ServiceID",
                keyValue: 6,
                columns: new[] { "CreatedAt", "Description", "EstimatedDuration", "Price", "ServiceName" },
                values: new object[] { new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8782), "Full body wax and polish", 60, 2000m, "Waxing" });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 1,
                columns: new[] { "SettingValue", "UpdatedAt" },
                values: new object[] { "Splash City Carwash", new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8329) });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8346));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8348));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "SettingID",
                keyValue: 4,
                columns: new[] { "SettingValue", "UpdatedAt" },
                values: new object[] { "Thank you for choosing Splash City!", new DateTime(2026, 3, 6, 22, 22, 51, 227, DateTimeKind.Local).AddTicks(8350) });
        }
    }
}
