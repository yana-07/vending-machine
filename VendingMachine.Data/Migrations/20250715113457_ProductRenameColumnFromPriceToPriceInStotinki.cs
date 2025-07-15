using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendingMachine.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductRenameColumnFromPriceToPriceInStotinki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Products",
                newName: "PriceInStotinki");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PriceInStotinki",
                table: "Products",
                newName: "Price");
        }
    }
}
