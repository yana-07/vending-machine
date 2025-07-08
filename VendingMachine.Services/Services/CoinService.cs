using Microsoft.EntityFrameworkCore;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Context;
using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class CoinService(
    VendingMachineDbContext dbContext) : ICoinService
{
    public async Task DepositAsync(IEnumerable<byte> coinsValues)
    {
        foreach (var coinValue in coinsValues)
        {
            var coin = await GetByValueAsync(coinValue);
            coin.Quantity++;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Coin>> GetAllAsync() => 
        await dbContext.Coins.OrderByDescending(coin => coin.Value).ToListAsync();

    public async Task<ChangeDto> ReturnChange(
        IEnumerable<byte> insertedAndNotDepositedCoinsValues, 
        int changeToReturn)
    {
        List<byte> coinValuesReturned = [];
        int remainingChange = changeToReturn;

        foreach (var coinValue in insertedAndNotDepositedCoinsValues
            .OrderByDescending(coinValue => coinValue))
        { 
            if (remainingChange - coinValue >= 0)
            {
                coinValuesReturned.Add(coinValue);
                remainingChange -= coinValue;
            }
        }

        var coinValuesToDeposit = insertedAndNotDepositedCoinsValues.Except(coinValuesReturned);

        if (coinValuesToDeposit.Any())
        {
            await DepositAsync(coinValuesToDeposit);
        }

        var changeResult = new ChangeDto 
        { 
            CoinsReturned = coinValuesReturned,
            RemainingAmount = remainingChange 
        };

        if (remainingChange > 0)
        {
            var tempChangeResult = await GetCoinsForChangeAsync(remainingChange);
            changeResult.CoinsReturned.AddRange(tempChangeResult.CoinsReturned);
            changeResult.RemainingAmount = tempChangeResult.RemainingAmount;
        }

        return changeResult;
    }

    private async Task<Coin> GetByValueAsync(byte value) =>
        await dbContext.Coins.FirstOrDefaultAsync(coin => coin.Value == value) ??
        throw new CoinNotFoundException($"A coin with value {value} does not exist.", value);

    private async Task<ChangeDto> GetCoinsForChangeAsync(int change)
    {
        var coins = await GetAllAsync();
        int remainingChange = change;
        const int MinCoinQuantity = 50;

        var changeResult = new ChangeDto();

        foreach (var coin in coins)
        {
            if (coin.Quantity > MinCoinQuantity && 
                remainingChange - coin.Value >= 0)
            {
                remainingChange -= coin.Value;
                changeResult.CoinsReturned.Add(coin.Value);
                coin.Quantity--;
            }
        }

        await dbContext.SaveChangesAsync();

        changeResult.RemainingAmount = remainingChange;
        return changeResult;
    }
}
