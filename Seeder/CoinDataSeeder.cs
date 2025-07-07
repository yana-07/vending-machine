using VendingMachine.Data;
using VendingMachine.Models;

namespace VendingMachine.Seeder;

public class CoinDataSeeder(VendingMachineDbContext dbContext)
    : DataSeeder<Coin>(dbContext)
{
    public override string FilePath => "coinsSeedData.json";
}
