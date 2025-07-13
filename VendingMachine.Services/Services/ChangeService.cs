using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class ChangeService(ICoinService coinService) 
    : IChangeService
{
    public async Task<ChangeDto> GenerateChange(
        Dictionary<byte, int> insertedCoins,
        int changeToReturn)
    {
        var changeFromInsertedCoins = GenerateChangeFromInsertedCoins(
            insertedCoins, changeToReturn);

        Dictionary<byte, int> coinsToDeposit = [];

        foreach (var (value, quantity) in insertedCoins)
        {
            bool isForChange = changeFromInsertedCoins
                .ReturnedCoins
                .TryGetValue(value, out int quantityForChange);

            var quantityToDeposit = quantity;

            if (isForChange) quantityToDeposit -= quantityForChange;

            if (quantityToDeposit == 0) continue;

            if (coinsToDeposit.TryGetValue(value, out int _))
            {
                coinsToDeposit[value] += quantityToDeposit;
            }
            else
            {
                coinsToDeposit.Add(value, quantityToDeposit);
            }
        }

        if (coinsToDeposit.Count > 0)
        {
            await coinService.DepositAsync(coinsToDeposit);
        }

        var finalChange = new ChangeDto
        {
            ReturnedCoins = changeFromInsertedCoins.ReturnedCoins,
            RemainingChange = changeFromInsertedCoins.RemainingChange
        };

        if (changeFromInsertedCoins.RemainingChange > 0)
        {
            var changeFromInventory = await GenerateChangeFromInventory(
                changeFromInsertedCoins.RemainingChange);

            foreach (var (value, quantity) in changeFromInventory.ReturnedCoins)
            {
                if (finalChange.ReturnedCoins.TryGetValue(value, out int _))
                {
                    finalChange.ReturnedCoins[value] += quantity;
                }
                else
                {
                    finalChange.ReturnedCoins.Add(value, quantity);
                }
            }

            var changeValue = changeFromInventory
                .ReturnedCoins.Sum(coin => coin.Key * coin.Value);

            finalChange.RemainingChange -= changeValue;
        }

        return finalChange;
    }

    private static ChangeDto GenerateChangeFromInsertedCoins(
        Dictionary<byte, int> insertedCoins,
        int changeToReturn)
    {
        Dictionary<byte, int> returnedCoins = [];
        int remainingChange = changeToReturn;

        foreach (var (value, quantity) in insertedCoins
            .OrderByDescending(coin => coin.Key))
        {
            for (int i = 0; i < quantity && remainingChange - value >= 0; i++)
            {
                if (returnedCoins.TryGetValue(value, out int _))
                {
                    returnedCoins[value]++;
                }
                else
                {
                    returnedCoins.Add(value, 1);
                }

                remainingChange -= value;
            }

            if (remainingChange == 0) break;
        }

        return new ChangeDto
        {
            ReturnedCoins = returnedCoins,
            RemainingChange = remainingChange
        };
    }

    private async Task<ChangeDto> GenerateChangeFromInventory(
        int changeToreturn)
    {
        const int MinCoinQuantity = 10;
        int remainingChange = changeToreturn;

        var coins = await coinService.GetAllDescendingAsNoTrackingAsync();

        Dictionary<byte, int> returnedCoins = [];

        foreach (var coin in coins)
        {
            while (coin.Quantity > MinCoinQuantity &&
                remainingChange - coin.Value >= 0)
            {
                if (returnedCoins.TryGetValue(coin.Value, out int _))
                {
                    returnedCoins[coin.Value]++;
                }
                else
                {
                    returnedCoins.Add(coin.Value, 1);
                }

                remainingChange -= coin.Value;
            }
        }

        await coinService.DecreaseInventoryAsync(returnedCoins);

        return new ChangeDto
        {
            ReturnedCoins = returnedCoins,
            RemainingChange = remainingChange
        };
    }
}
