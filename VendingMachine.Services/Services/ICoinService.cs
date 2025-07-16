using System.Linq.Expressions;
using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface ICoinService
{
    Task<IEnumerable<Coin>> GetAllAsync(
    Expression<Func<Coin, bool>>? wherePredicate = null);

    Task<IEnumerable<CoinDto>> GetAllAsNoTrackingAsync(
        Expression<Func<Coin, byte>>? orderByDescExpression = null);

    Task DepositAsync(Dictionary<byte, int> coins);

    Task DepositAsync(byte value, int quantity);

    Task<OperationResult> DecreaseInventoryAsync(
        Dictionary<byte, int> coins);

    Task<OperationResult> DecreaseInventoryAsync(
        byte value, int quantity);

    byte ParseCoinValue(string value);
}
