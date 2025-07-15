using Microsoft.EntityFrameworkCore;
using VendingMachine.Common.Constants;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Models;
using VendingMachine.Data.Repositories;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class CoinService(
    IRepository<Coin> repository) : ICoinService
{
    public async Task DepositAsync(Dictionary<byte, int> coins)
    {
        foreach (var (value, quantity) in coins)
        {
            var coinToUpdate = await GetByValueAsync(value);

            coinToUpdate.Quantity += quantity;
        }

        await repository.SaveChangesAsync();
    }

    public async Task DepositAsync(CoinDto coin)
    {
        var coinToUpdate = await GetByValueAsync(coin.Value);

        coinToUpdate.Quantity += coin.Quantity;

        await repository.SaveChangesAsync();
    }

    public async Task DecreaseInventoryAsync(Dictionary<byte, int> coins)
    {
        foreach (var (value, quantity) in coins)
        {
            var coinToUpdate = await GetByValueAsync(value);

            if (coinToUpdate.Quantity - quantity < 0)
            {
                throw new InvalidOperationException(
                    $"Cannot decrease coin inventory " +
                    $"by {quantity} " +
                    $"(available: {coinToUpdate.Quantity}).");
            }

            coinToUpdate.Quantity -= quantity;
        }

        await repository.SaveChangesAsync();
    }

    public async Task DecreaseInventoryAsync(CoinDto coin)
    {
        var coinToUpdate = await GetByValueAsync(coin.Value);

        if (coinToUpdate.Quantity - coin.Quantity < 0)
        {
            throw new InvalidOperationException(
                $"Cannot decrease coin inventory " +
                $"by {coin.Quantity} " +
                $"(available: {coinToUpdate.Quantity}).");
        }

        coinToUpdate.Quantity -= coin.Quantity;

        await repository.SaveChangesAsync();
    }

    private async Task<Coin> GetByValueAsync(byte value)
    {
        return await repository
            .FirstOrDefaultAsync(coinEntity => coinEntity.Value == value) ??
            throw new CoinNotFoundException(value);
    }

    public async Task<IEnumerable<CoinDto>> GetAllDescendingAsNoTrackingAsync()
    {
        return await repository.AllAsNoTracking()
            .OrderByDescending(coin => coin.Value)
            .Select(coin => new CoinDto
            {
                Value = coin.Value,
                Quantity = coin.Quantity
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<CoinDto>> GetAllAsNoTrackingAsync()
    {
        return await repository.AllAsNoTracking()
            .Select(coin => new CoinDto
            {
                Value = coin.Value,
                Quantity = coin.Quantity
            })
            .ToListAsync();
    }

    public byte ParseCoinValue(string value)
    {
        string valueWithoutCurrency = value
            .Replace(CurrencyConstants.LevaSuffix, string.Empty)
            .Replace(CurrencyConstants.StotinkiSuffix, string.Empty);

        if (!byte.TryParse(valueWithoutCurrency, out var valueAsByte))
        {
            throw new InvalidOperationException(
                $"Invalid coin value: {value}.");
        }

        if (value.Contains(CurrencyConstants.LevaSuffix))
        {
            valueAsByte *= 100;
        }

        return valueAsByte;
    }
}
