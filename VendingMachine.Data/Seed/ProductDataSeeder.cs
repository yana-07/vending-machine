using VendingMachine.Data.Context;
using VendingMachine.Data.Models;

namespace VendingMachine.Data.Seed;

public class ProductDataSeeder(VendingMachineDbContext dbContext) 
    : DataSeeder<Product>(dbContext)
{
    protected override string FilePath => "productDataSeed.json";
}
