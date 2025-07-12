using Microsoft.EntityFrameworkCore;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Context;
using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class CoinService(
    VendingMachineDbContext dbContext) : ICoinService
{
    public async Task DepositAsync(Dictionary<byte, int> coins)
    {
        var coinValuesToDeposit = coins.Keys;

        var coinsToUpdate = await dbContext.Coins
            .Where(coin => coins.Keys.Contains(coin.Value))
            .ToListAsync();

        var missingCoins = coinValuesToDeposit
            .Except(coinsToUpdate.Select(coin => coin.Value));

        if (missingCoins.Any())
        {
            throw new CoinNotFoundException($"Coins with the following values " +
                $"do not exist: {string.Join(", ", missingCoins)}");
        }

        coinsToUpdate.ForEach(
            coin => coin.Quantity += coins[coin.Value]);

        await dbContext.SaveChangesAsync();
    }

    public async Task DepositAsync(CoinDto coin)
    {
        var coinToUpdate = await dbContext.Coins
            .FirstOrDefaultAsync(coinToUpdate => coinToUpdate.Value == coin.Value) ??
            throw new CoinNotFoundException($"A coin with value {coin.Value} does not exist.");

        coinToUpdate.Quantity += coin.Quantity;

        await dbContext.SaveChangesAsync();
    }

    public async Task DecreaseInventoryAsync(Dictionary<byte, int> coins)
    {
        var coinEntities = await dbContext.Coins
            .Where(coin => coins.Keys.Contains(coin.Value))
            .ToListAsync();

        coinEntities.ForEach(
            coin => coin.Quantity -= coins[coin.Value]);

        await dbContext.SaveChangesAsync();
    }

    public async Task DecreaseInventoryAsync(CoinDto coin)
    {
        var coinToUpdate = await dbContext.Coins
            .FirstOrDefaultAsync(coinToUpdate => coinToUpdate.Value == coin.Value) ?? 
            throw new CoinNotFoundException($"A coin with value {coin.Value} does not exist.");

        if (coinToUpdate.Quantity - coin.Quantity < 0)
        {
            throw new InvalidOperationException(
                $"Cannot collect {coin.Quantity} coins of value {coin.Value}. " +
                $"Quantity cannot fall below zero. (available: {coinToUpdate.Quantity}).");
        }

        coinToUpdate.Quantity -= coin.Quantity;

        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<CoinDto>> GetAllDescendingAsNoTrackingAsync()
    {
        return await dbContext.Coins
            .OrderByDescending(coin => coin.Value)
            .Select(coin => new CoinDto
            {
                Value = coin.Value,
                Quantity = coin.Quantity
            })
            .AsNoTracking()
            .ToListAsync();
    }
}
