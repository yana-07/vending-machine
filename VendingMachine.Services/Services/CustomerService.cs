using Microsoft.Extensions.Options;
using VendingMachine.Common.Helpers;
using VendingMachine.Services.Configuration;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class CustomerService(
    IUserInteractor userInteractor,
    IProductService productsService,
    IOptions<CoinsOptions> coinsConfiguration,
    ITablePrinter<ProductPrintDto> tablePrinter)
    : ICustomerService
{
    private const string CancellationCommand = "cancel";
    private const string FinishCommand = "finish";

    public async Task ShowProductsAsync()
    {
        var products = await productsService.GetAllAsync();

        tablePrinter.Print(
            products.Select(
                product => new ProductPrintDto
                {
                    Name = product.Name,
                    Code = product.Code,
                    Price = product.PriceInStotinki,
                }));
    }

    public async Task<ProductRequestDto> RequestProductCodeAsync()
    {
        var productCodes = await productsService.GetAllCodesAsync();

        while (true)
        {
            userInteractor.ShowMessage($"""Select a product by its code or type "{CancellationCommand}" to abort.""");

            var input = userInteractor.ReadInput();

            if (string.IsNullOrEmpty(input))
                continue;

            if (input.Equals(CancellationCommand, StringComparison.OrdinalIgnoreCase))
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

            userInteractor.ShowMessage("Invalid product code.");
        }
    }

    public CoinRequestDto RequestCoin()
    {
        var allowedCoinValues = coinsConfiguration.Value.AllowedCoins.Select(byte.Parse);
        var coinRequestResult = new CoinRequestDto();

        while (true)
        {
            userInteractor.ShowMessage($"""Insert a coin, or type "{FinishCommand}" to proceed or "{CancellationCommand}" to abort.""");
            var input = userInteractor.ReadInput();

            if (string.IsNullOrEmpty(input))
                continue;

            if (input.Equals(CancellationCommand, StringComparison.OrdinalIgnoreCase))
            {
                coinRequestResult.IsCancelled = true;

                return coinRequestResult;
            }

            if (input.Equals(FinishCommand, StringComparison.OrdinalIgnoreCase))
            {
                coinRequestResult.IsFinished = true;

                return coinRequestResult;
            }

            if (byte.TryParse(input, out var coinValue) && allowedCoinValues.Contains(coinValue))
            {
                coinRequestResult.IsValid = true;
                coinRequestResult.Values.Add(coinValue);
                userInteractor.ShowMessage($"Total inserted: " +
                    $"{coinRequestResult.Values.Sum(coinValue => coinValue) / (decimal)100:F1}lv");

                continue;
            }

            coinRequestResult.IsValid = false;
            userInteractor.ShowMessage("Invalid coin.");
        }
    }
}
