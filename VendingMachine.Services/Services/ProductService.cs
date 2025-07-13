using Microsoft.EntityFrameworkCore;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Models;
using VendingMachine.Data.Context;
using VendingMachine.Services.DTOs;
using VendingMachine.Common.Constants;

namespace VendingMachine.Services.Services;

public class ProductService(
    VendingMachineDbContext dbContext)
    : IProductService
{
    public async Task<IEnumerable<ProductDto>> GetAllAsNoTrackingAsync()
    {
        return await dbContext.Products
            .AsNoTracking()
            .Select(product => new ProductDto
            {
                Name = product.Name,
                Code = product.Code,
                Price = product.Price,
                Quantity = product.Quantity
            })
            .ToListAsync();
    }


    public async Task<IEnumerable<string>> GetAllCodesAsync()
    {
        return await dbContext.Products
            .Select(product => product.Code)
            .ToListAsync();
    }

    public async Task<Product> GetByCodeAsync(string code)
    {
        return await dbContext.Products
            .FirstOrDefaultAsync(product => product.Code == code) ??
            throw new ProductNotFoundException(code);
    }
  
    public async Task DecreaseInventory(string code)
    {
        var product = await GetByCodeAsync(code);

        if (product.Quantity == 0)
        {
            throw new InvalidOperationException(
                $"Product with code {code} is out of stock.");
        }

        product.Quantity--;

        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateQuantityAsync(ProductQuantityUpdateDto product)
    {
        if (product.Quantity > ProductConstants.MaxQuantity)
        {
            throw new InvalidOperationException(
                $"Product quantity cannot exceed {ProductConstants.MaxQuantity}");
        }

        if (product.Quantity < ProductConstants.MinQuantity)
        {
            throw new InvalidOperationException(
                $"Product quantity cannot be less than {ProductConstants.MinQuantity}.");
        }

        var existingProduct = await GetByCodeAsync(product.Code);
        existingProduct.Quantity = product.Quantity;
        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveAsync(string code)
    {
        var existingProduct = await GetByCodeAsync(code);
        dbContext.Products.Remove(existingProduct); 
        await dbContext.SaveChangesAsync();
    }

    public async Task AddAsync(ProductDto product)
    {
        var exists = await dbContext.Products
            .AnyAsync(existingProduct => existingProduct.Code == product.Code);

        if (exists)
        {
            throw new InvalidOperationException(
                $"Product with code {product.Code} already exists.");
        }

        if (product.Quantity > ProductConstants.MaxQuantity)
        {
            throw new InvalidOperationException(
                $"Product quantity cannot exceed {ProductConstants.MaxQuantity}");
        }

        if (product.Quantity < ProductConstants.MinQuantity)
        {
            throw new InvalidOperationException(
                $"Product quantity cannot be less than {ProductConstants.MinQuantity}.");
        }

        dbContext.Products.Add(
            new Product
            { 
                Code = product.Code,
                Name = product.Name,
                Quantity = product.Quantity,
                Price = product.Price
            });

        await dbContext.SaveChangesAsync();
    }

    public async Task UpdatePriceAsync(ProductPriceUpdateDto product)
    {
        var existingProduct = await GetByCodeAsync(product.Code);
        existingProduct.Price = product.Price;
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> CanAddAsync()
    {
        var productsCount = await dbContext.Products.CountAsync();

        return productsCount < VendingMachineConstants.SlotLimit;
    }
}
