using Microsoft.EntityFrameworkCore;
using VendingMachine.Data.Models;

namespace VendingMachine.Data.Context;

public class VendingMachineDbContext(
    DbContextOptions<VendingMachineDbContext> options) 
    : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Coin> Coins { get; set; }
}
