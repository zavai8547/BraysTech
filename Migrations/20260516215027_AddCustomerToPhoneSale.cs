using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BraysTech.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerToPhoneSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneSaleItems_Customers_CustomerID",
                table: "PhoneSaleItems");

            migrationBuilder.DropIndex(
                name: "IX_PhoneSaleItems_CustomerID",
                table: "PhoneSaleItems");

            migrationBuilder.DropColumn(
                name: "CustomerID",
                table: "PhoneSaleItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerID",
                table: "PhoneSaleItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSaleItems_CustomerID",
                table: "PhoneSaleItems",
                column: "CustomerID");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneSaleItems_Customers_CustomerID",
                table: "PhoneSaleItems",
                column: "CustomerID",
                principalTable: "Customers",
                principalColumn: "CustomerID");
        }
    }
}
