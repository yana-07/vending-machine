using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendingMachine.Data.Migrations;

/// <inheritdoc />
public partial class RenameProductColumnsAndChangeType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Price",
            table: "Products");

        migrationBuilder.AlterColumn<string>(
            name: "Code",
            table: "Products",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte),
            oldType: "INTEGER");

        migrationBuilder.AddColumn<int>(
            name: "PriceInStotinki",
            table: "Products",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PriceInStotinki",
            table: "Products");

        migrationBuilder.AlterColumn<byte>(
            name: "Code",
            table: "Products",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AddColumn<decimal>(
            name: "Price",
            table: "Products",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);
    }
}
