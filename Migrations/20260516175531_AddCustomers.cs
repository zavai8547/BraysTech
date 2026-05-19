using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BraysTech.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerID",
                table: "PhoneSales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerID",
                table: "PhoneSaleItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FullName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalPurchases = table.Column<int>(type: "int", nullable: false),
                    TotalSpent = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSales_CustomerID",
                table: "PhoneSales",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSaleItems_CustomerID",
                table: "PhoneSaleItems",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneSaleItems_Customers_CustomerID",
                table: "PhoneSaleItems",
                column: "CustomerID",
                principalTable: "Customers",
                principalColumn: "CustomerID");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneSales_Customers_CustomerID",
                table: "PhoneSales",
                column: "CustomerID",
                principalTable: "Customers",
                principalColumn: "CustomerID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneSaleItems_Customers_CustomerID",
                table: "PhoneSaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PhoneSales_Customers_CustomerID",
                table: "PhoneSales");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_PhoneSales_CustomerID",
                table: "PhoneSales");

            migrationBuilder.DropIndex(
                name: "IX_PhoneSaleItems_CustomerID",
                table: "PhoneSaleItems");

            migrationBuilder.DropColumn(
                name: "CustomerID",
                table: "PhoneSales");

            migrationBuilder.DropColumn(
                name: "CustomerID",
                table: "PhoneSaleItems");
        }
    }
}
