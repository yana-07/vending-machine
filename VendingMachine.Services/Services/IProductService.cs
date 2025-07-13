using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface IProductService
{
    Task<Product> GetByCodeAsync(string code);

    Task<IEnumerable<ProductDto>> GetAllAsNoTrackingAsync();

    Task<IEnumerable<string>> GetAllCodesAsync();

    Task DecreaseInventory(string code);

    Task UpdateQuantityAsync(ProductQuantityUpdateDto product);

    Task UpdatePriceAsync(ProductPriceUpdateDto product);

    Task RemoveAsync(string code);

    Task AddAsync(ProductDto product);

    Task<bool> CanAddAsync();
}
