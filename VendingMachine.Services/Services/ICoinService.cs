using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface ICoinService
{
    Task DepositAsync(Dictionary<byte, int> coins);
    Task DepositAsync(CoinDto coin);
    Task DecreaseInventoryAsync(Dictionary<byte, int> coins);
    Task<IEnumerable<CoinDto>> GetAllDescendingAsNoTrackingAsync();
}
