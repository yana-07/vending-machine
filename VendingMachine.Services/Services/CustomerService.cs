using Spectre.Console;
using System.Linq;
using VendingMachine.Common.Constants;
using VendingMachine.Common.Enums;
using VendingMachine.Common.Exceptions;
using VendingMachine.Common.Helpers;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class CustomerService(
    IUserInteractor userInteractor,
    IProductService productService,
    IChangeService changeService,
    ITablePrinter tablePrinter,
    IAnsiConsole ansiConsole)
    : ICustomerService
{
    public async Task ServeCustomerAsync()
    {
        await ShowProductsAsync();

        await ProcessTransactionAsync();
    }

    private async Task ShowProductsAsync()
    {
        var products = await productService.GetAllAsNoTrackingAsync();

        tablePrinter.Print(
            products.Select(
                product => new ProductPrintDto
                {
                    Name = product.Name,
                    Code = product.Code,
                    Price = $"{product.PriceInStotinki / (decimal)100:F2}lv",
                }));
    }

    private async Task ProcessTransactionAsync()
    {
        ProductRequestResultDto? productRequestResult = null;
        CoinRequestResultDto? coinRequestResult = null;
        Dictionary<byte, int> insertedCoins = [];

        while (true)
        {
            if (coinRequestResult is null)
            {
                coinRequestResult = RequestCoins(
                    insertedCoins.Sum(coin => coin.Key * coin.Value));

                foreach (var coin in coinRequestResult.InsertedCoins)
                {
                    if (insertedCoins.TryGetValue(coin.Key, out int _))
                    {
                        insertedCoins[coin.Key] += coin.Value;
                    }
                    else
                    {
                        insertedCoins.Add(coin.Key, coin.Value);
                    }
                }

                if (coinRequestResult.IsCancelled || !coinRequestResult.IsValid)
                {
                    if (coinRequestResult.InsertedCoins.Count > 0)
                        ReturnInserted(insertedCoins);
                    return;
                }
            }

            if (productRequestResult is null)
            {
                productRequestResult = await RequestProductAsync();
                if (productRequestResult.IsCancelled)
                {
                    ReturnInserted(insertedCoins);
                    return;
                }
            }

            try
            {
                if (await IsPurchaseProcessed(productRequestResult.ProductCode, insertedCoins))
                    return;
            }
            catch (Exception ex) when (
                ex is ProductNotFoundException || ex is InvalidOperationException)
            {
                userInteractor.ShowMessage(ex.Message);
                productRequestResult = null;
                continue;
            }

            coinRequestResult = null;
        }
    }

    private CoinRequestResultDto RequestCoins(int alreadyInsertedAmount)
    {
        var coinRequestResult = new CoinRequestResultDto();

        string[] choices = [
            .. CoinConstants.AllowedCoins
                .Select(coin => coin.ToString()),
            .. Enum.GetValues<CustomerCommands>()
                .Select(command => command.ToString())];

        while (true)
        {
            var userInput = ansiConsole.Prompt(
               new SelectionPrompt<string>()
                   .Title($"Insert a coin or select a command.")
                   .AddChoices(choices));

            if (userInput.Equals(
                nameof(CustomerCommands.Cancel),
                StringComparison.OrdinalIgnoreCase))
            {
                coinRequestResult.IsCancelled = true;

                return coinRequestResult;
            }

            if (userInput.Equals(
                nameof(CustomerCommands.Continue),
                StringComparison.OrdinalIgnoreCase))
            {
                coinRequestResult.IsFinished = true;

                return coinRequestResult;
            }

            if (byte.TryParse(userInput, out var coinValue) &&
                CoinConstants.AllowedCoins.Contains(coinValue))
            {
                coinRequestResult.IsValid = true;
                if (coinRequestResult.InsertedCoins.TryGetValue(coinValue, out int _))
                {
                    coinRequestResult.InsertedCoins[coinValue]++;
                }
                else
                {
                    coinRequestResult.InsertedCoins.Add(coinValue, 1);
                }
               
                var justInsertedAmount = coinRequestResult
                    .InsertedCoins
                    .Sum(coin => coin.Key * coin.Value);
                var totalInsertedAmount = justInsertedAmount + alreadyInsertedAmount;

                userInteractor.ShowMessage($"Total inserted: " +
                    $"{totalInsertedAmount / (decimal)100:F2}lv");
            }
        }
    }

    private void ReturnInserted(Dictionary<byte, int> coins)
    {
        userInteractor.ShowMessage($"Returning inserted coins...");
        foreach (var (nominalValue, quantity) in coins)
        {
            for (int i = 0; i < quantity; i++)
            {
                userInteractor.ShowMessage(nominalValue.ToString());
            }
        }
    }

    private async Task<ProductRequestResultDto> RequestProductAsync()
    {
        var productCodes = await productService.GetAllCodesAsync();
        string[] choices = [.. productCodes, nameof(CustomerCommands.Cancel)];

        var userInput = ansiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Select a product code or a command.")
                .AddChoices(choices));

        bool isCancelled = userInput.Equals(
            nameof(CustomerCommands.Cancel),
            StringComparison.OrdinalIgnoreCase);

        return new ProductRequestResultDto
        {
            IsCancelled = isCancelled,
            IsValid = !isCancelled,
            ProductCode = isCancelled ? string.Empty : userInput
        };
    }

    private async Task<bool> IsPurchaseProcessed(
        string productCode, Dictionary<byte, int> insertedCoins)
    {
        SellProductDto? sellProductResult = null;

        sellProductResult = await TrySellProductAsync(
            productCode,
            insertedCoins.Sum(coin => coin.Key * coin.Value));

        if (sellProductResult?.IsSuccess == true &&
            sellProductResult?.RemainingToInsert == 0)
        {
            await productService.DecreaseInventory(productCode);

            userInteractor.ShowMessage($"Dispensing product with code {productCode}...");

            var changeResult = await changeService.GenerateChange(
                insertedCoins, sellProductResult.ChangeToReturn);

            if (changeResult.RemainingChange > 0)
            {
                userInteractor.ShowMessage($"The following amount could not be returned: " +
                    $"{changeResult.RemainingChange / (decimal)100:F2}lv.");
            }

            if (changeResult.ReturnedCoins.Count > 0)
            {
                ReturnInserted(changeResult.ReturnedCoins);              
            }

            return true;
        }
        else if (sellProductResult?.IsSuccess == false &&
            sellProductResult?.RemainingToInsert > 0)
        {
            userInteractor.ShowMessage(
                $"Insufficient funds. Insert " +
                $"{sellProductResult.RemainingToInsert / (decimal)100:F2}lv more to continue.");
        }

        return false;
    }

    private async Task<SellProductDto> TrySellProductAsync(string code, int coinsValuesSum)
    {
        var product = await productService.GetByCodeAsync(code) ??
            throw new ProductNotFoundException($"A product with code {code} does not exist.", code);

        if (product.Quantity == 0)
        {
            throw new InvalidOperationException($"The product with code {code} ({product.Name}) " +
                $"is out of stock. Please select another one or contact the vendor.");
        }

        if (product.PriceInStotinki <= coinsValuesSum)
        {
            return new SellProductDto
            {
                IsSuccess = true,
                ChangeToReturn = coinsValuesSum - product.PriceInStotinki
            };
        }

        return new SellProductDto
        {
            IsSuccess = false,
            RemainingToInsert = product.PriceInStotinki - coinsValuesSum
        };
    }
}
