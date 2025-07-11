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

        foreach (var (nominalValue, _) in insertedCoins)
        {
            var quantityToDeposit = changeFromInsertedCoins
                .ReturnedCoins
                .TryGetValue(nominalValue, out int returnedQuantity) ? returnedQuantity : 0;

            if (quantityToDeposit == 0) continue;

            if (coinsToDeposit.TryGetValue(nominalValue, out int _))
            {
                coinsToDeposit[nominalValue] += quantityToDeposit;
            }
            else
            {
                coinsToDeposit.Add(nominalValue, quantityToDeposit);
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

            foreach (var (nominalValue, quantity) in changeFromInventory.ReturnedCoins)
            {
                if (finalChange.ReturnedCoins.TryGetValue(nominalValue, out int _))
                {
                    finalChange.ReturnedCoins[nominalValue] += quantity;
                }
                else
                {
                    finalChange.ReturnedCoins.Add(nominalValue, quantity);
                }
            }

            finalChange.RemainingChange += changeFromInventory.RemainingChange;
        }

        return finalChange;
    }

    private static ChangeDto GenerateChangeFromInsertedCoins(
        Dictionary<byte, int> insertedCoins,
        int changeToReturn)
    {
        Dictionary<byte, int> returnedCoins = [];
        int remainingChange = changeToReturn;

        foreach (var (nominalValue, quantity) in insertedCoins
            .OrderByDescending(coin => coin.Key))
        {
            for (int i = 0; i < quantity && remainingChange - nominalValue >= 0; i++)
            {
                if (returnedCoins.TryGetValue(nominalValue, out int _))
                {
                    returnedCoins[nominalValue]++;
                }
                else
                {
                    returnedCoins.Add(nominalValue, 1);
                }

                remainingChange -= nominalValue;
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
