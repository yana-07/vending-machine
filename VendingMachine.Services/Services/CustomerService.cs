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
                    PriceInStotinki = product.PriceInStotinki,
                    Quantity = product.Quantity,
                }));
    }

    private async Task ProcessTransactionAsync()
    {
        ProductRequestResultDto? productRequestResult = null;
        CoinRequestResultDto? coinRequestResult = null;
        Dictionary<byte, int> totalInsertedCoins = [];

        while (true)
        {
            if (coinRequestResult is null)
            {
                var totalInsertedAmount = totalInsertedCoins.Sum(coin => coin.Key * coin.Value);

                coinRequestResult = await RequestCoins(totalInsertedAmount);

                UpdateTotalInsertedCoins(coinRequestResult.InsertedCoins, totalInsertedCoins);               

                if (coinRequestResult.IsCancelled)
                {
                    if (totalInsertedCoins.Count > 0) 
                        ReturnInserted(totalInsertedCoins);
                    break;
                }
            }

            if (productRequestResult is null)
            {
                productRequestResult = await RequestProductAsync();
                if (productRequestResult.IsCancelled)
                {
                    ReturnInserted(totalInsertedCoins);
                    break;
                }
            }

            try
            {
                var purchaseResult = await TryCompletePurchase(
                    productRequestResult.ProductCode, totalInsertedCoins);

                if (!purchaseResult.IsSuccess)
                {
                    DisplaySaleError(purchaseResult.ErrorMessage);

                    productRequestResult = null;
                    coinRequestResult = null;
                }
                else
                {
                    break;           
                }
            }
            catch (Exception ex) 
                when (ex is ProductNotFoundException ||
                    ex is CoinNotFoundException)
            {
                logger.LogError(ex, "An error occurred in {Method}.",
                    nameof(ProcessTransactionAsync));
                ansiConsole.MarkupLine($"[red]{ex.Message}[/]");
                productRequestResult = null;
                continue;
            }
        }

        if (totalInsertedCoins.Count > 0)
        {
            ansiConsole.MarkupLine("Press any key to continue.");
            Console.ReadKey();
        }   
    }

    private async Task<CoinRequestResultDto> RequestCoins(
        int alreadyInsertedAmount)
    {
        var coinRequestResult = new CoinRequestResultDto();

        var coins = await coinService.GetAllAsNoTrackingAsync();

        while (true)
        {
            var selection = PromptForCoin(
                coins.Select(coin => coin.Denomination),
                alreadyInsertedAmount,
                coinRequestResult.InsertedCoins.Count);

            UpdateCoinRequestResultIfSpecialCommandSelected(selection, coinRequestResult);

            if (coinRequestResult.IsCancelled || coinRequestResult.IsFinished)
            {
                return coinRequestResult;
            }

            var coinValue = coinService.ParseCoinValue(selection);

            if (CoinConstants.AllowedCoins.Contains(coinValue))
            {
                coinRequestResult.IsValid = true;
                UpdateCurrentlyInsertedCoins(coinRequestResult, coinValue);
            }

            var totalInserted = 
                alreadyInsertedAmount + 
                coinRequestResult.InsertedCoins
                    .Sum(coin => coin.Key * coin.Value);

            DisplayTotalInserted(totalInserted);
        }
    }

    private string PromptForCoin(
        IEnumerable<string> coinChoices, 
        int totalInsertedAmount, int insertedCoinsCount)
    {
        var selectionPrompt = new SelectionPrompt<string>()
           .Title("Insert a coin:")
           .AddChoices(coinChoices);

        if (totalInsertedAmount > 0 || insertedCoinsCount > 0)
        {
            selectionPrompt.AddChoice(nameof(CustomerCommand.Continue));
        }

        selectionPrompt.AddChoice(nameof(CustomerCommand.Cancel));

        return ansiConsole.Prompt(selectionPrompt);
    }

    private static void UpdateCoinRequestResultIfSpecialCommandSelected(
        string selection, CoinRequestResultDto coinRequestResult)
    {
        if (selection == nameof(CustomerCommand.Cancel))
        {
            coinRequestResult.IsCancelled = true;
        }

        if (selection == nameof(CustomerCommand.Continue))
        {
            coinRequestResult.IsFinished = true;
        }
    }

    private static void UpdateCurrentlyInsertedCoins(
        CoinRequestResultDto coinRequestResult, byte coinValue)
    {
        if (coinRequestResult.InsertedCoins.TryGetValue(coinValue, out int _))
        {
            coinRequestResult.InsertedCoins[coinValue]++;
        }
        else
        {
            coinRequestResult.InsertedCoins.Add(coinValue, 1);
        }
    }

    public void DisplayTotalInserted(int amount)
    {
        ansiConsole.MarkupLine(
            "[green]Total inserted: " +
            $"{amount / 100m:F2}" +
            $"{CurrencyConstants.LevaSuffix}[/]");
    }

    private static void UpdateTotalInsertedCoins(
        Dictionary<byte, int> currentlyInserted,
        Dictionary<byte, int> totalInserted)
    {
        foreach (var coin in currentlyInserted)
        {
            if (totalInserted.TryGetValue(coin.Key, out int _))
            {
                totalInserted[coin.Key] += coin.Value;
            }
            else
            {
                totalInserted.Add(coin.Key, coin.Value);
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
        string[] choices = [.. productCodes, nameof(CustomerCommand.Cancel)];

        var selection = ansiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Select product code:")
                .AddChoices(choices));

        bool isCancelled = selection == nameof(CustomerCommand.Cancel);

        return new ProductRequestResultDto
        {
            IsCancelled = isCancelled,
            IsValid = !isCancelled,
            ProductCode = isCancelled ? string.Empty : selection
        };
    }

    private async Task<OperationResult> TryCompletePurchase(
        string productCode, Dictionary<byte, int> insertedCoins)
    {
        int insertedAmount = insertedCoins
            .Sum(coin => coin.Key * coin.Value);

        var sellProductResult = await TrySellProductAsync(productCode, insertedAmount);

        if (!sellProductResult.IsSuccess || sellProductResult.RemainingToInsert > 0)
        {
            return OperationResult.Failure(sellProductResult.ErrorMessage);
        }

        var operationResult = await productService.DecreaseInventoryAsync(productCode);
        if (!operationResult.IsSuccess) return operationResult;

        var product = await productService.GetByCodeAsync(productCode);

        DisplaySaleSuccess(product.Name);

        var changeResult = await changeService.GenerateChange(
            insertedCoins, sellProductResult.ChangeToReturn);

        HandleChangeResult(changeResult);

        return OperationResult.Success();
    }

    private async Task<SellProductDto> TrySellProductAsync(
       string code, int insertedAmount)
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

        if (product.PriceInStotinki <= insertedAmount)
        {
            return new SellProductDto
            {
                IsSuccess = true,
                ChangeToReturn = insertedAmount - product.PriceInStotinki
            };
        }

        int remainingToInsert = product.PriceInStotinki - insertedAmount;

        return new SellProductDto
        {
            IsSuccess = false,
            RemainingToInsert = remainingToInsert,
            ErrorMessage = "Insufficient funds. Insert " +
                $"{remainingToInsert / 100m:F2}" +
                $"{CurrencyConstants.LevaSuffix} more to continue."
        };
    }

    private void DisplaySaleError(string? saleErrorMessage)
    {
        ansiConsole.MarkupLine($"[red]{saleErrorMessage ??
            "Impossible to sell product."}[/]");
    }

    private void DisplaySaleSuccess(string productName)
    {
        ansiConsole.MarkupLine($"[green]Dispensing \"{productName}\"...[/]");
    }

    private void HandleChangeResult(ChangeDto changeResult)
    {
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
    }
}
