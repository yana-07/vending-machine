using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using VendingMachine.Common.Constants;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Models;
using VendingMachine.Data.Repositories;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class CoinService(
    IRepository<Coin> repository) : ICoinService
{
    public async Task<IEnumerable<Coin>> GetAllAsync(
        Expression<Func<Coin, bool>>? wherePredicate = null)
    {
        var coins = repository.AllAsNoTracking();

        if (wherePredicate is not null)
        {
            coins = coins.Where(wherePredicate);
        }

        return await coins.ToListAsync();
    }

    public async Task<IEnumerable<CoinDto>> GetAllAsNoTrackingAsync(
        Expression<Func<Coin, byte>>? orderByDescExpression = null)
    {
        var coins = repository.AllAsNoTracking();

        if (orderByDescExpression is not null)
        {
            coins = coins.OrderByDescending(orderByDescExpression);
        }

        return await coins
            .Select(coin => new CoinDto
            {
                Value = coin.Value,
                Quantity = coin.Quantity
            })
            .ToListAsync();
    }

    public async Task<Dictionary<string, byte>> GetAllAsDenominationToValueMap()
    {
        var coins = await GetAllAsNoTrackingAsync();

        return coins
            .ToDictionary(
                coin => coin.Denomination,
                coin => coin.Value);
    }

    public async Task DepositAsync(Dictionary<byte, int> coins)
    {
        var coinValues = coins.Keys.ToList();

        var coinsToUpdate = await GetAllAsync(
            coin => coinValues.Contains(coin.Value));

        var invalidCoins = coinValues.Except(
            coinsToUpdate.Select(coin => coin.Value));

        if (invalidCoins.Any())
        {
            throw new CoinNotFoundException(invalidCoins);
        }

        foreach (var coinToUpdate in coinsToUpdate)
        {
            coinToUpdate.Quantity += coins[coinToUpdate.Value];
        }

        await repository.SaveChangesAsync();
    }

    public async Task DepositAsync(byte value, int quantity)
    {
        var coinToUpdate = await GetByValueAsync(value);

        coinToUpdate.Quantity += quantity;

        await repository.SaveChangesAsync();
    }

    public async Task<OperationResult> DecreaseInventoryAsync(
        Dictionary<byte, int> coins)
    {
        var coinValues = coins.Keys;

        var coinsToUpdate = await GetAllAsync(
           coin => coinValues.Contains(coin.Value));

        var invalidCoins = coinValues.Except(
            coinsToUpdate.Select(coin => coin.Value));

        if (invalidCoins.Any())
        {
            throw new CoinNotFoundException(invalidCoins);
        }

        foreach (var coinToUpdate in coinsToUpdate)
        {
            var quantityValidationResult = ValidateQuantity(
                coinToUpdate.Quantity, coins[coinToUpdate.Value]);

            if (!quantityValidationResult.IsSuccess)
                return quantityValidationResult;

            coinToUpdate.Quantity -= coins[coinToUpdate.Value];
        }

        await repository.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task<OperationResult> DecreaseInventoryAsync(
        byte value, int quantity)
    {
        var coinToUpdate = await GetByValueAsync(value);

        var quantityValidationResult = ValidateQuantity(
            coinToUpdate.Quantity, quantity);

        if (!quantityValidationResult.IsSuccess) 
            return quantityValidationResult;

        coinToUpdate.Quantity -= quantity;

        await repository.SaveChangesAsync();

        return OperationResult.Success();
    }

    private async Task<Coin> GetByValueAsync(byte value)
    {
        return await repository
            .FirstOrDefaultAsync(coinEntity => coinEntity.Value == value) ??
            throw new CoinNotFoundException(value);
    }

    private static OperationResult ValidateQuantity(
        int current, int decreaseBy)
    {
        if (current - decreaseBy < 0)
        {
            return OperationResult.Failure(
                $"Cannot decrease coin inventory " +
                $"by {decreaseBy} " +
                $"(available: {current}).");
        }

        return OperationResult.Success();
    }
}
