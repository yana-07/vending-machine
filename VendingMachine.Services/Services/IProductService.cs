using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface IProductService
{
    Task AddAsync(Product product);
    Task UpdateAsync(string name);
    Task RemoveAsync(string name);
    Task<Product> GetByCodeAsync(string code);
    Task<IEnumerable<ProductDto>> GetAllAsNoTrackingAsync();
    Task<IEnumerable<string>> GetAllCodesAsync();
    Task DecreaseInventory(string code);
}
