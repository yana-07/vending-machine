using Microsoft.EntityFrameworkCore;
using VendingMachine.Common.Constants;
using VendingMachine.Data.Models;

namespace VendingMachine.Data.Context;

public class VendingMachineDbContext(
    DbContextOptions<VendingMachineDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products { get; set; }

    public DbSet<Coin> Coins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<Product>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_ProductQuantityRange",
                $"[Quantity] BETWEEN {ProductConstants.MinQuantity} AND {ProductConstants.MaxQuantity}"));

        modelBuilder
            .Entity<Coin>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_CoinAllowedValues", $"[Value] IN ({string.Join(',', CoinConstants.AllowedCoins)})"));
    }
}
