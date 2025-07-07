using Microsoft.EntityFrameworkCore;
using VendingMachine.Models;

namespace VendingMachine.Data;

public class VendingMachineDbContext(
    DbContextOptions<VendingMachineDbContext> options) 
    : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Coin> Coins { get; set; }
}
