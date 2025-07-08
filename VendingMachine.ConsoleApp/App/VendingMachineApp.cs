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
        var userRole = userService.RequestUserRole();

        if (userRole.Equals(nameof(UserRoles.Customer), StringComparison.OrdinalIgnoreCase))
        {
            await customerService.ServeCustomerAsync();
        }
        else if (userRole.Equals(nameof(UserRoles.Vendor), StringComparison.OrdinalIgnoreCase))
        {
            await vendorService.ServeVendorAsync();
        }
    }
}
