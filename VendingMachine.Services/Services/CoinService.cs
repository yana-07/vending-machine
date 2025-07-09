using Microsoft.EntityFrameworkCore;
using VendingMachine.Data.Context;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class CoinService(
    VendingMachineDbContext dbContext) : ICoinService
{
    public async Task DepositAsync(IEnumerable<byte> coinValues)
    {
        var groupedCoins = GroupCoinsByValues(coinValues);

        var coinEntities = await dbContext.Coins
            .Where(coin => groupedCoins.Keys.Contains(coin.Value))
            .ToListAsync();

        coinEntities.ForEach(
            coin => coin.Quantity += groupedCoins[coin.Value]);

        await dbContext.SaveChangesAsync();
    }

    public async Task DecreaseInventoryAsync(IEnumerable<byte> coinValues)
    {
        var groupedCoins = GroupCoinsByValues(coinValues);

        var coinEntities = await dbContext.Coins
            .Where(coin => groupedCoins.Keys.Contains(coin.Value))
            .ToListAsync();

        coinEntities.ForEach(
            coin => coin.Quantity -= groupedCoins[coin.Value]);

        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<CoinDto>> GetAllDescendingAsNoTrackingAsync() =>
        await dbContext.Coins
        .OrderByDescending(coin => coin.Value)
        .Select(coin => new CoinDto
        {
            Value = coin.Value,
            Quantity = coin.Quantity
        })
        .AsNoTracking()
        .ToListAsync();

    private static Dictionary<byte, int> GroupCoinsByValues(IEnumerable<byte> coinValues) => 
        coinValues.GroupBy(coinValue => coinValue)
            .ToDictionary(grouping =>
                grouping.Key,
                grouping => grouping.Count());
}
