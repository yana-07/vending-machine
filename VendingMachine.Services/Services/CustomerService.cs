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

        await Task.Delay(1000);

        Console.Clear();
        Console.WriteLine("\x1b[3J");
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
                    Price = $"{product.Price / (decimal)100:F2}lv",
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

                if (coinRequestResult.IsCancelled)
                {
                    if (insertedCoins.Count > 0) 
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
                if (await TryProcessPurchase(productRequestResult.ProductCode, insertedCoins))
                    return;
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

            coinRequestResult = null;
        }
    }

    private CoinRequestResultDto RequestCoins(int alreadyInsertedAmount)
    {
        var coinRequestResult = new CoinRequestResultDto();

        var choices = CoinConstants.AllowedCoins.Select(coin => coin.ToString());

        while (true)
        {
            var selectionPrompt = new SelectionPrompt<string>()
               .Title("Insert a coin:")
               .AddChoices(
                    CoinConstants.AllowedCoins
                    .Select(coin => coin.ToString()));

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

            if (byte.TryParse(selection, out var coinValue) &&
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

                ansiConsole.MarkupLine(
                    "[green]Total inserted: " +
                    $"{totalInsertedAmount / (decimal)100:F2}lv[/]");
            }
        }
    }

    private void ReturnInserted(Dictionary<byte, int> coins)
    {
        ansiConsole.MarkupLine("[blue]Returning inserted coins...[/]");
        foreach (var (nominalValue, quantity) in coins)
        {
            for (int i = 0; i < quantity; i++)
            {
                ansiConsole.MarkupLine(nominalValue.ToString());
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
        SellProductDto? sellProductResult = null;

        sellProductResult = await TrySellProductAsync(
            productCode,
            insertedCoins.Sum(coin => coin.Key * coin.Value));

        if (sellProductResult?.IsSuccess == true &&
            sellProductResult?.RemainingToInsert == 0)
        {
            await productService.DecreaseInventory(productCode);

            ansiConsole.MarkupLine("[green]Dispensing product...[/]");

            var changeResult = await changeService.GenerateChange(
                insertedCoins, sellProductResult.ChangeToReturn);

            if (changeResult.RemainingChange > 0)
            {
                ansiConsole.MarkupLine(
                    "[yellow]The following amount could not be returned: " +
                    $"{changeResult.RemainingChange / (decimal)100:F2}lv.[/]");
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
            ansiConsole.MarkupLine(
                "[red]Insufficient funds. Insert " +
                $"{sellProductResult.RemainingToInsert / (decimal)100:F2}lv more to continue.[/]");
        }

        return false;
    }

    private async Task<SellProductDto> TrySellProductAsync(string code, int coinsValuesSum)
    {
        var product = await productService.GetByCodeAsync(code);

        if (product.Quantity == 0)
        {
            throw new InvalidOperationException(
                $"Product \"{product.Name}\" with code {code} " +
                $"is out of stock. Please select another one or contact the vendor.");
        }

        if (product.Price <= coinsValuesSum)
        {
            return new SellProductDto
            {
                IsSuccess = true,
                ChangeToReturn = coinsValuesSum - product.Price
            };
        }

        return new SellProductDto
        {
            IsSuccess = false,
            RemainingToInsert = product.Price - coinsValuesSum
        };
    }
}
