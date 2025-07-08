using VendingMachine.Data.Models;

namespace VendingMachine.Services.Services;

public interface IProductService
{
    Task AddAsync(Product product);
    Task UpdateAsync(string name);
    Task RemoveAsync(string name);
    Task<Product> GetByCodeAsync(string code);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<string>> GetAllCodesAsync();
    Task DecreaseInventory(string code);
}
