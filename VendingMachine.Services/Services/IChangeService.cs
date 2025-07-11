using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface IChangeService
{
    public Task<ChangeDto> GenerateChange(
       Dictionary<byte, int> insertedCoins,
       int changeToReturn);
}
