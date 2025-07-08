using Microsoft.EntityFrameworkCore;
using VendingMachine.Data.Context;

namespace VendingMachine.Services.Services;

public class CoinService(
    VendingMachineDbContext dbContext) : ICoinService
{
    public async Task Insert(byte value)
    {
        var coin = await dbContext.Coins
            .FirstOrDefaultAsync(coin => coin.Value == value);

        if (coin is not null)
        {
            ++coin.Quantity;

            await dbContext.SaveChangesAsync();
        }
    }

    public async Task ReturnInserted(byte value)
    {
        var coin = await dbContext.Coins
            .FirstOrDefaultAsync(coin => coin.Value == value);

        if (coin is not null)
        {
            --coin.Quantity;

            await dbContext.SaveChangesAsync();
        }
    }
}
