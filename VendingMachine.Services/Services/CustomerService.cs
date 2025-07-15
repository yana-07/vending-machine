using Microsoft.Extensions.Logging;
using Spectre.Console;
using VendingMachine.Common.Constants;
using VendingMachine.Common.Enums;
using VendingMachine.Common.Exceptions;
using VendingMachine.Common.Helpers;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class CustomerService(
    IProductService productService,
    ICoinService coinService,
    IChangeService changeService,
    ITablePrinter tablePrinter,
    IAnsiConsole ansiConsole,
    ILogger<CustomerService> logger)
    : ICustomerService
{
    public async Task ServeCustomerAsync()
    {
        await ShowProductsAsync();

        await ProcessTransactionAsync();

        Console.Clear();
        Console.WriteLine("\x1b[3J");
    }

    private async Task ShowProductsAsync()
    {
        var products = await productService.GetAllAsNoTrackingAsync();

        tablePrinter.Print(
            products.Select(
                product => new ProductDto
                {
                    Name = product.Name,
                    Code = product.Code,
                    Price = product.Price,
                    Quantity = product.Quantity,
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
                coinRequestResult = await RequestCoins(
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

                if (coinRequestResult.IsCancelled)
                {
                    if (insertedCoins.Count > 0) 
                        ReturnInserted(insertedCoins);
                    break;
                }
            }

            if (productRequestResult is null)
            {
                productRequestResult = await RequestProductAsync();
                if (productRequestResult.IsCancelled)
                {
                    ReturnInserted(insertedCoins);
                    break;
                }
            }

            try
            {
                if (await TryProcessPurchase(productRequestResult.ProductCode, insertedCoins))
                    break;
            }
            catch (Exception ex) 
                when (ex is ProductNotFoundException || 
                    ex is InvalidOperationException ||
                    ex is CoinNotFoundException)
            {
                logger.LogError(ex, nameof(ProcessTransactionAsync));
                ansiConsole.MarkupLine($"[red]{ex.Message}[/]");
                productRequestResult = null;
                continue;
            }

            productRequestResult = null;
            coinRequestResult = null;
        }

        if (insertedCoins.Count > 0)
        {
            ansiConsole.MarkupLine("Press any key to continue.");
            Console.ReadKey();
        }   
    }

    private async Task<CoinRequestResultDto> RequestCoins(int alreadyInsertedAmount)
    {
        var coinRequestResult = new CoinRequestResultDto();

        var coins = await coinService.GetAllAsNoTrackingAsync();

        while (true)
        {
            var selectionPrompt = new SelectionPrompt<string>()
               .Title("Insert a coin:")
               .AddChoices(coins.Select(coin => coin.Denomination));

            if (alreadyInsertedAmount > 0 || coinRequestResult.InsertedCoins.Count > 0)
            {
                selectionPrompt.AddChoice(nameof(CustomerCommands.Continue));
            }

            selectionPrompt.AddChoice(nameof(CustomerCommands.Cancel));

            var selection = ansiConsole.Prompt(selectionPrompt);

            if (selection == nameof(CustomerCommands.Cancel))
            {
                coinRequestResult.IsCancelled = true;

                return coinRequestResult;
            }

            if (selection == nameof(CustomerCommands.Continue))
            {
                return coinRequestResult;
            }

            var coinValue = coinService.ParseCoinValue(selection);

            if (CoinConstants.AllowedCoins.Contains(coinValue))
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

                ansiConsole.MarkupLine(
                    "[green]Total inserted: " +
                    $"{totalInsertedAmount / 100m:F2}" +
                    $"{CurrencyConstants.LevaSuffix}[/]");
            }
        }
    }

    private void ReturnInserted(Dictionary<byte, int> coins)
    {
        var coinDtos = coins.Select(coin => 
            new CoinDto { Value = coin.Key, Quantity = coin.Value });

        ansiConsole.MarkupLine("[blue]Returning inserted coins...[/]");

        foreach (var coinDto in coinDtos)
        {
            for (int i = 0; i < coinDto.Quantity; i++)
            {
                ansiConsole.MarkupLine(coinDto.Denomination);
            }
        }
    }

    private async Task<ProductRequestResultDto> RequestProductAsync()
    {
        var productCodes = await productService.GetAllCodesAsync();
        string[] choices = [.. productCodes, nameof(CustomerCommands.Cancel)];

        var selection = ansiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Select product code:")
                .AddChoices(choices));

        bool isCancelled = selection == nameof(CustomerCommands.Cancel);

        return new ProductRequestResultDto
        {
            IsCancelled = isCancelled,
            IsValid = !isCancelled,
            ProductCode = isCancelled ? string.Empty : selection
        };
    }

    private async Task<bool> TryProcessPurchase(
        string productCode, Dictionary<byte, int> insertedCoins)
    {
        SellProductDto sellProductResult = await TrySellProductAsync(
            productCode, insertedCoins.Sum(coin => coin.Key * coin.Value));

        if (sellProductResult.IsSuccess &&
            sellProductResult.RemainingToInsert == 0)
        {
            await productService.DecreaseInventoryAsync(productCode);

            var product = await productService.GetByCodeAsync(productCode);

            ansiConsole.MarkupLine($"[green]Dispensing \"{product.Name}\"...[/]");

            var changeResult = await changeService.GenerateChange(
                insertedCoins, sellProductResult.ChangeToReturn);

            if (changeResult.RemainingChange > 0)
            {
                ansiConsole.MarkupLine(
                    "[yellow]The following amount could not be returned: " +
                    $"{changeResult.RemainingChange / 100m:F2}" +
                    $"{CurrencyConstants.LevaSuffix}.[/]");
            }

            if (changeResult.ReturnedCoins.Count > 0)
            {
                ReturnInserted(changeResult.ReturnedCoins);              
            }
            else
            {
                ansiConsole.MarkupLine("[yellow]No change to return.[/]");
            }

            return true;
        }
        else
        {
            ansiConsole.MarkupLine($"[red]{sellProductResult.ErrorMessage ?? 
                "Impossible to sell product."}[/]");
        }

        return false;
    }

    private async Task<SellProductDto> TrySellProductAsync(
        string code, int coinsValuesSum)
    {
        var product = await productService.GetByCodeAsync(code);

        if (product.Quantity == 0)
        {
            return new SellProductDto
            {
                IsSuccess = false,
                ErrorMessage = $"Product \"{product.Name}\" with code {code} " +
                $"is out of stock. Please select another one or contact the vendor."
            };
        }

        if (product.Price <= coinsValuesSum)
        {
            return new SellProductDto
            {
                IsSuccess = true,
                ChangeToReturn = coinsValuesSum - product.Price
            };
        }

        int remainingToInsert = product.Price - coinsValuesSum;

        return new SellProductDto
        {
            IsSuccess = false,
            RemainingToInsert = remainingToInsert,
            ErrorMessage = "[red]Insufficient funds. Insert " +
                $"{remainingToInsert / 100m:F2}" +
                $"{CurrencyConstants.LevaSuffix} more to continue.[/]"
        };
    }
}
