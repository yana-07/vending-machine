using VendingMachine.Common.Enums;
using VendingMachine.Services.Services;

namespace VendingMachine.ConsoleApp.App;

public class VendingMachineApp(
    IUserService userService,
    ICustomerService customerService,
    IVendorService vendorService) 
    : IVendingMachineApp
{
    public async Task RunAsync()
    {
        while (true)
        {
            var userRole = userService.RequestUserRole();

            if (userRole == UserRoles.Customer)
            {
                await customerService.ServeCustomerAsync();
            }
            else if (userRole == UserRoles.Vendor)
            {
                await vendorService.ServeVendorAsync();
            }
        }
    }
}
