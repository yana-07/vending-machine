using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendingMachine.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductAndCoinIndexColumnAndAddValueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductQuantityRange",
                table: "Products",
                sql: "[Quantity] BETWEEN 0 AND 10");

            migrationBuilder.CreateIndex(
                name: "IX_Coins_Value",
                table: "Coins",
                column: "Value",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CoinAllowedValues",
                table: "Coins",
                sql: "[Value] IN (10,20,50,100,200)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Code",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductQuantityRange",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Coins_Value",
                table: "Coins");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CoinAllowedValues",
                table: "Coins");
        }
    }
}
