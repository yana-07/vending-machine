using Microsoft.EntityFrameworkCore;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Models;
using VendingMachine.Data.Context;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class ProductService(
    VendingMachineDbContext dbContext)
    : IProductService
{
    public Task AddAsync(Product product)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(string name)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(string name)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsNoTrackingAsync() =>
        await dbContext.Products
        .AsNoTracking()
        .Select(product => new ProductDto
        {
            Name = product.Name,
            Code = product.Code,
            PriceInStotinki = product.PriceInStotinki,
        })
        .ToListAsync();

    public async Task<IEnumerable<string>> GetAllCodesAsync() => 
        await dbContext.Products
        .Select(product => product.Code)
        .ToListAsync();

    public async Task<Product> GetByCodeAsync(string code) => 
        await dbContext.Products.FirstOrDefaultAsync(product => product.Code == code) ?? 
            throw new ProductNotFoundException($"Product with code {code} does not exist.", code);
  

    public async Task DecreaseInventory(string code)
    {
        var product = await GetByCodeAsync(code);

        product.Quantity--;

        await dbContext.SaveChangesAsync();
    }
}
