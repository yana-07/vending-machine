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
    ICoinService coinService,
    IOptions<CoinsSettings> coinsConfiguration,
    ITablePrinter<ProductPrintDto> tablePrinter)
    : ICustomerService
{
    public async Task ServeCustomerAsync()
    {
        await ShowProductsAsync();

        var coinRequestResult = RequestCoin();
        if (coinRequestResult.IsCancelled || !coinRequestResult.IsValid)
        {
            userInteractor.ShowMessage($"Returning inserted coins..." +
                $"{Environment.NewLine}{string.Join(
                    Environment.NewLine, coinRequestResult.CoinsValues)}");
            return;
        }

        var productRequestResult = await RequestProductAsync();

        if (productRequestResult.IsCancelled)
        {
            userInteractor.ShowMessage($"Returning inserted coins..." +
                $"{Environment.NewLine}{string.Join(
                    Environment.NewLine, coinRequestResult.CoinsValues)}");
            return;
        }

        var sellProductResult = await TrySellProductAsync(
            productRequestResult.ProductCode,
            coinRequestResult.CoinsValues.Sum(value => value));

        if (sellProductResult.IsSuccess)
        {
            await productService.DecreaseInventory(productRequestResult.ProductCode);
            var changeResult = await coinService.ReturnChange(
                coinRequestResult.CoinsValues, sellProductResult.ChangeToReturn);

            if (changeResult.RemainingAmount > 0)
            {
                userInteractor.ShowMessage($"The following amount could not be returned: " +
                    $"{changeResult.RemainingAmount / (decimal)100:F2}lv.");
            }

            if (changeResult.CoinsReturned.Count > 0)
            {
                userInteractor.ShowMessage($"Returning your change..." +
                    $"{Environment.NewLine}{string.Join(
                        Environment.NewLine, changeResult.CoinsReturned)}");
            }
        }
        else
        {
            userInteractor.ShowMessage($"Insufficient balance. Operation cancelled. " +
                $"Returning your inserted coins: {Environment.NewLine}{string.Join(
                        Environment.NewLine, coinRequestResult.CoinsValues)}");
        }
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
            ChangeToReturn = 0
        };
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

    private async Task<ProductRequestDto> RequestProductAsync()
    {
        var productCodes = await productService.GetAllCodesAsync();

        while (true)
        {
            userInteractor.ShowMessage($"Select a product by its code " +
                $"or type \"{MachineInteractionCommands.Cancel}\" to abort.");

            var input = userInteractor.ReadInput();

            if (string.IsNullOrEmpty(input))
                continue;

            if (input.Equals(nameof(MachineInteractionCommands.Cancel), StringComparison.OrdinalIgnoreCase))
            {
                return new ProductRequestDto
                {
                    IsCancelled = true,
                    IsValid = false,
                    ProductCode = string.Empty
                };
            }

            if (productCodes.Contains(input))
            {
                return new ProductRequestDto
                {
                    IsCancelled = false,
                    IsValid = true,
                    ProductCode = input
                };
            }

            userInteractor.ShowMessage("Invalid product code or command.");
        }
    }

    private CoinRequestDto RequestCoin()
    {
        var allowedCoinValues = coinsConfiguration.Value.AllowedCoins;//.Select(byte.Parse);
        var coinRequestResult = new CoinRequestDto();

        while (true)
        {
            userInteractor.ShowMessage($"Insert a coin (valid coins: [{string.Join(", ", allowedCoinValues)}]), " +
                $"or type \"{MachineInteractionCommands.Finish}\" " +
                $"to proceed or \"{MachineInteractionCommands.Cancel}\" to abort.");
            var input = userInteractor.ReadInput();

            if (string.IsNullOrEmpty(input))
                continue;

            if (input.Equals(nameof(MachineInteractionCommands.Cancel), StringComparison.OrdinalIgnoreCase))
            {
                coinRequestResult.IsCancelled = true;

                return coinRequestResult;
            }

            if (input.Equals(nameof(MachineInteractionCommands.Finish), StringComparison.OrdinalIgnoreCase))
            {
                coinRequestResult.IsFinished = true;

                return coinRequestResult;
            }

            if (byte.TryParse(input, out var coinValue) && allowedCoinValues.Contains(coinValue))
            {
                coinRequestResult.IsValid = true;
                coinRequestResult.CoinsValues.Add(coinValue);
                userInteractor.ShowMessage($"Total inserted: " +
                    $"{coinRequestResult.CoinsValues.Sum(coinValue => coinValue) / (decimal)100:F2}lv");

                continue;
            }

            coinRequestResult.IsValid = false;
            userInteractor.ShowMessage("Invalid coin or command.");
        }
    }
}
