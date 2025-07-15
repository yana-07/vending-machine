using Microsoft.Extensions.Logging;
using Spectre.Console;
using VendingMachine.Common.Enums;
using VendingMachine.Services.Services;

namespace VendingMachine.ConsoleApp.App;

public class VendingMachineApp(
    IUserService userService,
    ICustomerService customerService,
    IVendorService vendorService,
    IAnsiConsole ansiConsole,
    ILogger<VendingMachineApp> logger) 
    : IVendingMachineApp
{
    public async Task RunAsync()
    {
        try
        {
            while (true)
            {
                var userRole = userService.RequestUserRole();

                if (userRole == UserRole.Customer)
                {
                    await customerService.ServeCustomerAsync();
                }
                else if (userRole == UserRole.Vendor)
                {
                    await vendorService.ServeVendorAsync();
                }
            }
        }
        catch (Exception ex)
        {
            var message = "An unexpected error occurred.";
            ansiConsole.MarkupLine($"[red]{message}[/]");
            logger.LogError(ex, message);
        }
    }
}
