using System.Text.Json;
using VendingMachine.Data;

namespace VendingMachine.Seeder;

public abstract class DataSeeder<T>(
    VendingMachineDbContext dbContext) : IDataSeeder
{
    public abstract string FilePath { get; }

    public async Task SeedAsync()
    {
        if (dbContext.Products.Any())
        {
            return;
        }

        var entities = DeserializeData(await ReadData(FilePath));

        if (entities is null)
        {
            return;
        }

        entities.ForEach(entity =>
        {
            if (entity is not null)
            {
                dbContext.Add(entity);
            }
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<string> ReadData(string filePath) =>
        await File.ReadAllTextAsync(filePath);

    private static List<T>? DeserializeData(string jsonData)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<List<T>>(
            jsonData, jsonSerializerOptions);
    }
}
