using VendingMachine.Data.Context;
using VendingMachine.Data.Models;

namespace VendingMachine.Data.Seed;

public class CoinDataSeeder(VendingMachineDbContext dbContext)
    : DataSeeder<Coin>(dbContext)
{
    protected override string FilePath => Path.Combine(
        AppContext.BaseDirectory, "coinDataSeed.json");
}
