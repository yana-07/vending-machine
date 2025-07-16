using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface IProductService
{
    Task<Product> GetByCodeAsync(string code);

    Task<IEnumerable<ProductDto>> GetAllAsNoTrackingAsync();

    Task<IEnumerable<string>> GetAllCodesAsync();

    Task<OperationResult> DecreaseInventoryAsync(string code);

    Task<OperationResult> UpdateQuantityAsync(ProductQuantityUpdateDto product);

    Task<OperationResult> UpdatePriceAsync(ProductPriceUpdateDto product);

    Task RemoveAsync(string code);

    Task<OperationResult> AddAsync(ProductDto product);

    Task<bool> CanAddAsync();
}
