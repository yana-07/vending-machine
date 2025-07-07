using VendingMachine.Data;
using VendingMachine.Models;

namespace VendingMachine.Seeder;

public class ProductDataSeeder(VendingMachineDbContext dbContext) 
    : DataSeeder<Product>(dbContext)
{
    public override string FilePath => "productsSeedData.json";
}
