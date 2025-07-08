using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public interface IProductService
{
    Task AddAsync(Product product);
    Task UpdateAsync(string name);
    Task RemoveAsync(string name);
    Task<BuyProductDto> BuyAsync(string code, int coinsValuesSum);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<string>> GetAllCodesAsync();
}
