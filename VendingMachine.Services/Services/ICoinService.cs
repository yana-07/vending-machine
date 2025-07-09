using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface ICoinService
{
    Task DepositAsync(IEnumerable<byte> coinValues);
    Task DecreaseInventoryAsync(IEnumerable<byte> coinValues);
    Task<IEnumerable<CoinDto>> GetAllDescendingAsNoTrackingAsync();
}
