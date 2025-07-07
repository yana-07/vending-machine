namespace VendingMachine.Seeder;

public class SeederCoordinator(
    IEnumerable<IDataSeeder> dataSeeders)
{
    public async Task SeedAllAsync()
    {
        var seedingTasks = new List<Task>();

        seedingTasks.AddRange(
            dataSeeders.Select(
                dataSeeder => dataSeeder.SeedAsync()));

        await Task.WhenAll(seedingTasks);
    }
}
