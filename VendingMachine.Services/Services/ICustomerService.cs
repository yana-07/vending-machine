using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface ICustomerService
{
    Task ServeCustomerAsync();
}
