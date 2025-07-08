using Microsoft.EntityFrameworkCore;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;
using VendingMachine.Data.Context;

namespace VendingMachine.Services.Services;

public class ProductService(
    VendingMachineDbContext dbContext)
    : IProductService
{
    public Task AddAsync(Product product)
    {
        throw new NotImplementedException();
    }

    public async Task<BuyProductDto> BuyAsync(string code, int coinsValuesSum)
    {
        var product = await dbContext
            .Products
            .FirstOrDefaultAsync(product => product.Code == code) ?? 
            throw new ProductNotFoundException($"A product with code {code} does not exist.", code);

        if (product.Quantity == 0)
        {
            throw new InvalidOperationException($"The product with code {code} ({product.Name}) " +
                $"is out of stock. Please select another one or contact the vendor.");
        }

        if (product.PriceInStotinki <= coinsValuesSum)
        {
            product.Quantity--;

            return new BuyProductDto
            {
                IsSuccess = true,
                ChangeToReturn = coinsValuesSum - product.PriceInStotinki
            };
        }

        return new BuyProductDto
        {
            IsSuccess = false,
            ChangeToReturn = 0
        };
    }

    public Task RemoveAsync(string name)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(string name)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Product>> GetAllAsync() =>
        await dbContext.Products.ToListAsync();

    public async Task<IEnumerable<string>> GetAllCodesAsync() => 
        await dbContext.Products.Select(product => product.Code).ToListAsync();
}
