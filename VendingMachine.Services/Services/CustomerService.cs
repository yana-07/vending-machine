using Microsoft.Extensions.Options;
using VendingMachine.Common.Enums;
using VendingMachine.Common.Exceptions;
using VendingMachine.Common.Helpers;
using VendingMachine.Services.Configuration;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class CustomerService(
    IUserInteractor userInteractor,
    IProductService productService,
    IChangeService changeService,
    IOptions<CoinsSettings> coinsConfiguration,
    ITablePrinter<ProductPrintDto> tablePrinter)
    : ICustomerService
{
    public async Task ServeCustomerAsync()
    {
        await ShowProductsAsync();

        await ProcessTransactionAsync();
    }

    private async Task ShowProductsAsync()
    {
        var products = await productService.GetAllAsync();

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
        List<byte> insertedCoins = [];

        while (true)
        {
            if (coinRequestResult is null)
            {
                coinRequestResult = RequestCoins(insertedCoins.Sum(coin => coin));
                insertedCoins.AddRange(coinRequestResult.InsertedCoins);

                if (coinRequestResult.IsCancelled || !coinRequestResult.IsValid)
                {
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

        while (true)
        {
            userInteractor.ShowMessage($"Insert a coin " +
                $"(valid coins: [{string.Join(", ", coinsConfiguration.Value.AllowedCoins)}]), " +
                $"or type \"{MachineInteractionCommands.Continue}\" " +
                $"to proceed or \"{MachineInteractionCommands.Cancel}\" to abort.");

            var input = userInteractor.ReadInput();

            if (string.IsNullOrEmpty(input))
            {
                userInteractor.ShowMessage("Invalid coin or command.");

                continue;
            }

            if (input.Equals(
                nameof(MachineInteractionCommands.Cancel),
                StringComparison.OrdinalIgnoreCase))
            {
                coinRequestResult.IsCancelled = true;

                return coinRequestResult;
            }

            if (input.Equals(
                nameof(MachineInteractionCommands.Continue),
                StringComparison.OrdinalIgnoreCase))
            {
                coinRequestResult.IsFinished = true;

                return coinRequestResult;
            }

            if (byte.TryParse(input, out var coinValue) &&
                coinsConfiguration.Value.AllowedCoins.Contains(coinValue))
            {
                coinRequestResult.IsValid = true;
                coinRequestResult.InsertedCoins.Add(coinValue);
                userInteractor.ShowMessage($"Total inserted: " +
                    $"{(coinRequestResult.InsertedCoins.Sum(
                        coinValue => coinValue) + alreadyInsertedAmount) / (decimal)100:F2}lv");

                continue;
            }

            coinRequestResult.IsValid = false;
            userInteractor.ShowMessage("Invalid coin or command.");
        }
    }

    private void ReturnInserted(List<byte> coins)
    {
        userInteractor.ShowMessage($"Returning inserted coins..." +
            $"{Environment.NewLine}{string.Join(
                Environment.NewLine, coins)}");
    }

    private async Task<ProductRequestResultDto> RequestProductAsync()
    {
        var productCodes = await productService.GetAllCodesAsync();

        while (true)
        {
            userInteractor.ShowMessage($"Select a product by its code " +
                $"or type \"{MachineInteractionCommands.Cancel}\" to abort.");

            var input = userInteractor.ReadInput();

            if (string.IsNullOrEmpty(input))
            {
                userInteractor.ShowMessage("Invalid product code or command.");
                continue;
            }

            if (input.Equals(
                nameof(MachineInteractionCommands.Cancel),
                StringComparison.OrdinalIgnoreCase))
            {
                return new ProductRequestResultDto
                {
                    IsCancelled = true,
                    IsValid = false,
                    ProductCode = string.Empty
                };
            }

            if (productCodes.Contains(input))
            {
                return new ProductRequestResultDto
                {
                    IsCancelled = false,
                    IsValid = true,
                    ProductCode = input
                };
            }

            userInteractor.ShowMessage("Invalid product code or command.");
        }
    }

    private async Task<bool> IsPurchaseProcessed(
        string productCode, List<byte> insertedCoins)
    {
        SellProductDto? sellProductResult = null;

        sellProductResult = await TrySellProductAsync(
            productCode,
            insertedCoins.Sum(coin => coin));

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
                userInteractor.ShowMessage($"Returning your change..." +
                    $"{Environment.NewLine}{string.Join(
                        Environment.NewLine, changeResult.ReturnedCoins)}");
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
