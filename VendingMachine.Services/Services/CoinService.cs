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
        foreach (var (value, quantity) in coins)
        {
            var coinToUpdate = await GetByValueAsync(value);

            coinToUpdate.Quantity += quantity;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DepositAsync(CoinDto coin)
    {
        var coinToUpdate = await GetByValueAsync(coin.Value);

        coinToUpdate.Quantity += coin.Quantity;

        await dbContext.SaveChangesAsync();
    }

    public async Task DecreaseInventoryAsync(Dictionary<byte, int> coins)
    {
        foreach (var (value, quantity) in coins)
        {
            var coinToUpdate = await GetByValueAsync(value);

            if (coinToUpdate.Quantity - quantity < 0)
            {
                throw new InvalidOperationException(
                    $"Cannot decrease inventory of coin " +
                    $"with value {coinToUpdate.Value} by {quantity}. " +
                    $"Quantity cannot fall below zero. " +
                    $"(available: {coinToUpdate.Quantity}).");
            }

            coinToUpdate.Quantity -= quantity;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DecreaseInventoryAsync(CoinDto coin)
    {
        var coinToUpdate = await GetByValueAsync(coin.Value);

        if (coinToUpdate.Quantity - coin.Quantity < 0)
        {
            throw new InvalidOperationException(
                $"Cannot decrease inventory of coin " +
                $"with value {coinToUpdate.Value} by {coin.Quantity}. " +
                $"Quantity cannot fall below zero. " +
                $"(available: {coinToUpdate.Quantity}).");
        }

        coinToUpdate.Quantity -= coin.Quantity;

        await dbContext.SaveChangesAsync();
    }

    private async Task<Coin> GetByValueAsync(byte value)
    {
        return await dbContext.Coins
            .FirstOrDefaultAsync(coinEntity => coinEntity.Value == value) ??
            throw new CoinNotFoundException(value);
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
