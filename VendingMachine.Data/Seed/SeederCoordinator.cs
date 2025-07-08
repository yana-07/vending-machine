namespace VendingMachine.Data.Seed;

public class SeederCoordinator(
    IEnumerable<IDataSeeder> dataSeeders)
{
    public async Task SeedAllAsync()
    {
        foreach (var dataSeeder in dataSeeders)
        {
            await dataSeeder.SeedAsync();
        }
    }
}
