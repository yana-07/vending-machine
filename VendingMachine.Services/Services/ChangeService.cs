using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class ChangeService(ICoinService coinService) 
    : IChangeService
{
    public async Task<ChangeDto> GenerateChange(
        IEnumerable<byte> insertedCoinValues,
        int changeToReturn)
    {
        var changeFromInsertedCoins = GenerateChangeFromInsertedCoins(
            insertedCoinValues, changeToReturn);

        var coinValuesToDeposit = insertedCoinValues
            .Except(changeFromInsertedCoins.ReturnedCoins);

        if (coinValuesToDeposit.Any())
        {
            await coinService.DepositAsync(coinValuesToDeposit);
        }

        var finalChange = new ChangeDto
        {
            ReturnedCoins = [.. changeFromInsertedCoins.ReturnedCoins],
            RemainingChange = changeFromInsertedCoins.RemainingChange
        };

        if (changeFromInsertedCoins.RemainingChange > 0)
        {
            var changeFromInventory = await GenerateChangeFromInventory(
                changeFromInsertedCoins.RemainingChange);

            finalChange.ReturnedCoins.AddRange(changeFromInventory.ReturnedCoins);
            finalChange.RemainingChange += changeFromInventory.RemainingChange;
        }

        return finalChange;
    }

    private static ChangeDto GenerateChangeFromInsertedCoins(
        IEnumerable<byte> insertedCoinValues,
        int changeToReturn)
    {
        List<byte> returnedCoinValues = [];
        int remainingChange = changeToReturn;

        foreach (var coinValue in insertedCoinValues
            .OrderByDescending(coinValue => coinValue))
        {
            if (remainingChange - coinValue >= 0)
            {
                returnedCoinValues.Add(coinValue);
                remainingChange -= coinValue;
            }
        }

        return new ChangeDto
        {
            ReturnedCoins = returnedCoinValues,
            RemainingChange = remainingChange
        };
    }

    private async Task<ChangeDto> GenerateChangeFromInventory(
        int changeToreturn)
    {
        const int MinCoinQuantity = 10;
        int remainingChange = changeToreturn;

        var coins = await coinService.GetAllDescendingAsNoTrackingAsync();

        List<byte> returnedCoins = [];

        foreach (var coin in coins)
        {
            while (coin.Quantity > MinCoinQuantity &&
                remainingChange - coin.Value >= 0)
            {
                remainingChange -= coin.Value;
                returnedCoins.Add(coin.Value);
            }
        }

        await coinService.DecreaseInventoryAsync(returnedCoins);

        return new ChangeDto
        {
            ReturnedCoins = [.. returnedCoins],
            RemainingChange = remainingChange
        };
    }
}
