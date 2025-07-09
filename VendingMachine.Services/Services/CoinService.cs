using Microsoft.EntityFrameworkCore;
using VendingMachine.Data.Context;
using VendingMachine.Data.Models;

namespace VendingMachine.Services.Services;

public class CoinService(
    VendingMachineDbContext dbContext) : ICoinService
{
    public async Task DepositAsync(IEnumerable<byte> coinValues)
    {
        var groupedCoins = coinValues
            .GroupBy(coinValue => coinValue)
            .ToDictionary(grouping =>
                grouping.Key,
                grouping => grouping.Count());

        var coinEntities = await dbContext.Coins
            .Where(coin => groupedCoins.Keys.Contains(coin.Value))
            .ToListAsync();

        coinEntities
            .ForEach(coin => coin.Quantity += groupedCoins[coin.Value]);

        await dbContext.SaveChangesAsync();
    }

    public async Task DecreaseInventoryAsync(IEnumerable<byte> coinValues)
    {
        var groupedCoins = coinValues
           .GroupBy(coinValue => coinValue)
           .ToDictionary(grouping =>
               grouping.Key,
               grouping => grouping.Count());

        var coinEntities = await dbContext.Coins
            .Where(coin => groupedCoins.Keys.Contains(coin.Value))
            .ToListAsync();

        coinEntities
            .ForEach(coin => coin.Quantity -= groupedCoins[coin.Value]);

        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Coin>> GetAllDescendingAsNoTrackingAsync() =>
        await dbContext.Coins
        .OrderByDescending(coin => coin.Value)
        .AsNoTracking()
        .ToListAsync();
}
