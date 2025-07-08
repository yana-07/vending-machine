using VendingMachine.Data.Context;
using VendingMachine.Data.Models;

namespace VendingMachine.Data.Seed;

public class ProductDataSeeder(VendingMachineDbContext dbContext) 
    : DataSeeder<Product>(dbContext)
{
    public override string FilePath => "productDataSeed.json";
}
