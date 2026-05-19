using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BraysTech.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneSaleItem_IMEIStock_PhoneStockID",
                table: "PhoneSaleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PhoneSaleItem_PhoneSales_SaleID",
                table: "PhoneSaleItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhoneSaleItem",
                table: "PhoneSaleItem");

            migrationBuilder.RenameTable(
                name: "PhoneSaleItem",
                newName: "PhoneSaleItems");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneSaleItem_SaleID",
                table: "PhoneSaleItems",
                newName: "IX_PhoneSaleItems_SaleID");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneSaleItem_PhoneStockID",
                table: "PhoneSaleItems",
                newName: "IX_PhoneSaleItems_PhoneStockID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhoneSaleItems",
                table: "PhoneSaleItems",
                column: "ItemID");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneSaleItems_IMEIStock_PhoneStockID",
                table: "PhoneSaleItems",
                column: "PhoneStockID",
                principalTable: "IMEIStock",
                principalColumn: "StockID");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneSaleItems_PhoneSales_SaleID",
                table: "PhoneSaleItems",
                column: "SaleID",
                principalTable: "PhoneSales",
                principalColumn: "SaleID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneSaleItems_IMEIStock_PhoneStockID",
                table: "PhoneSaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PhoneSaleItems_PhoneSales_SaleID",
                table: "PhoneSaleItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhoneSaleItems",
                table: "PhoneSaleItems");

            migrationBuilder.RenameTable(
                name: "PhoneSaleItems",
                newName: "PhoneSaleItem");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneSaleItems_SaleID",
                table: "PhoneSaleItem",
                newName: "IX_PhoneSaleItem_SaleID");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneSaleItems_PhoneStockID",
                table: "PhoneSaleItem",
                newName: "IX_PhoneSaleItem_PhoneStockID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhoneSaleItem",
                table: "PhoneSaleItem",
                column: "ItemID");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneSaleItem_IMEIStock_PhoneStockID",
                table: "PhoneSaleItem",
                column: "PhoneStockID",
                principalTable: "IMEIStock",
                principalColumn: "StockID");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneSaleItem_PhoneSales_SaleID",
                table: "PhoneSaleItem",
                column: "SaleID",
                principalTable: "PhoneSales",
                principalColumn: "SaleID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
