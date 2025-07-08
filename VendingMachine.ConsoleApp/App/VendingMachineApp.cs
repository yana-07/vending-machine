using Microsoft.Extensions.Options;
using VendingMachine.Services.Configuration;
using VendingMachine.Services.Services;

namespace VendingMachine.ConsoleApp.App;

public class VendingMachineApp(
    IOptions<UserRolesOptions> userRolesConfiguration,
    IUserInteractor userInteractor,
    ICustomerService customerInteractor,
    IProductService productService) 
    : IVendingMachineApp
{
    public async Task RunAsync()
    {
        var userRole = RequestUserRole();

        if (userRole.Equals("customer", StringComparison.OrdinalIgnoreCase))
        {
            await HandleCustomerRequest();
        }
        else if (userRole.Equals("vendor", StringComparison.OrdinalIgnoreCase))
        {
            await HandleVendorRequest();
        }
    }

    private string RequestUserRole()
    {
        string? userRole;

        while (true)
        {
            userInteractor.ShowMessage("Are you a customer or a vendor?");

            userRole = userInteractor.ReadInput();

            if (string.IsNullOrEmpty(userRole))
                continue;

            if (!userRolesConfiguration.Value.AllowedRoles.Contains(userRole.ToLowerInvariant()))
            {
                userInteractor.ShowMessage("Invalid user role.");
                continue;
            }

            return userRole;
        }
    }

    private async Task HandleCustomerRequest()
    {
        await customerInteractor.ShowProductsAsync();

        var coinRequestResult = customerInteractor.RequestCoin();
        if (coinRequestResult.IsCancelled || !coinRequestResult.IsValid)
        {
            userInteractor.ShowMessage("Returning inserted coins...");
            userInteractor.ShowMessage(string.Join(Environment.NewLine, coinRequestResult.Values));
            return;
        }

        var productRequestResult = await customerInteractor.RequestProductCodeAsync();

        if (productRequestResult.IsCancelled)
        {
            userInteractor.ShowMessage("Returning inserted coins...");
            userInteractor.ShowMessage(string.Join(Environment.NewLine, coinRequestResult.Values));
            return;
        }

        var buyProductResult = await productService.BuyAsync(
            productRequestResult.ProductCode,
            coinRequestResult.Values.Sum(value => value));

        if (buyProductResult.IsSuccess)
        {

        }
    }

    private Task HandleVendorRequest()
    {
        throw new NotImplementedException();
    }
}
