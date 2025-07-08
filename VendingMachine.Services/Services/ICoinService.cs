using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface ICoinService
{
    Task<IEnumerable<Coin>> GetAllAsync();
    Task DepositAsync(IEnumerable<byte> coinsValues);
    Task<ChangeDto> ReturnChange(
        IEnumerable<byte> insertedAndNotDepositedCoinsValues,
        int changeToReturn);
}
