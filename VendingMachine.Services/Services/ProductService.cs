using Microsoft.EntityFrameworkCore;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Models;
using VendingMachine.Services.DTOs;
using VendingMachine.Common.Constants;
using VendingMachine.Data.Repositories;

namespace VendingMachine.Services.Services;

public class ProductService(
    IRepository<Product> repository)
    : IProductService
{
    public async Task<Product> GetByCodeAsync(string code)
    {
        return await repository
            .FirstOrDefaultAsync(product => product.Code == code) ??
            throw new ProductNotFoundException(code);
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsNoTrackingAsync()
    {
        return await repository.AllAsNoTracking()
            .Select(product => new ProductDto
            {
                Name = product.Name,
                Code = product.Code,
                PriceInStotinki = product.PriceInStotinki,
                Quantity = product.Quantity
            })
            .ToListAsync();
    }


    public async Task<IEnumerable<string>> GetAllCodesAsync()
    {
        return await repository.AllAsNoTracking()
            .Select(product => product.Code)
            .ToListAsync();
    }
  
    public async Task<OperationResult> DecreaseInventoryAsync(string code)
    {
        var product = await GetByCodeAsync(code);

        if (product.Quantity == 0)
        {
            return OperationResult.Failure(
                $"Product with code {code} is out of stock.");
        }

        product.Quantity--;

        await repository.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateQuantityAsync(
        ProductQuantityUpdateDto product)
    {
        var validationResult = ValidateQuantity(product.Quantity);
        if (!validationResult.IsSuccess) return validationResult;

        var existingProduct = await GetByCodeAsync(product.Code);
        existingProduct.Quantity = product.Quantity;
        await repository.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task RemoveAsync(string code)
    {
        var existingProduct = await GetByCodeAsync(code);
        repository.Delete(existingProduct);
        await repository.SaveChangesAsync();
    }

    public async Task<OperationResult> AddAsync(ProductDto product)
    {
        var existingProduct = await repository.FirstOrDefaultAsync(
            productEntity => productEntity.Code == product.Code);

        if (existingProduct is not null)
        {
            return OperationResult.Failure(
                $"Product with code {product.Code} already exists.");
        }

        var validationResult = ValidateQuantity(product.Quantity);
        if (!validationResult.IsSuccess) return validationResult;

        validationResult = ValidatePrice(product.PriceInStotinki);
        if (!validationResult.IsSuccess) return validationResult;

        repository.Add(
            new Product
            { 
                Code = product.Code,
                Name = product.Name,
                Quantity = product.Quantity,
                PriceInStotinki = product.PriceInStotinki
            });

        await repository.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdatePriceAsync(
        ProductPriceUpdateDto product)
    {
        var validationResult = ValidatePrice(product.Price);
        if (!validationResult.IsSuccess) return validationResult;

        var existingProduct = await GetByCodeAsync(product.Code);
        existingProduct.PriceInStotinki = product.Price;
        await repository.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task<bool> CanAddAsync()
    {
        var productsCount = await repository.AllAsNoTracking().CountAsync();

        return productsCount < VendingMachineConstants.SlotLimit;
    }

    private static OperationResult ValidateQuantity(int quantity)
    {
        if (quantity > ProductConstants.MaxQuantity)
        {
            return OperationResult.Failure(
                $"Product quantity cannot exceed " +
                $"{ProductConstants.MaxQuantity}.");
        }

        // Check is currently redundand due to product quantity's
        // numeric data type (byte is unsigned) and minimum quantity = 0.
        // Remains in case minimum quantity is changed in the future.
        // Cannot be tested at this point.
        if (quantity < ProductConstants.MinQuantity)
        {
            OperationResult.Failure(
                $"Product quantity cannot be less than " +
                $"{ProductConstants.MinQuantity}.");
        }

        return OperationResult.Success();
    }

    private static OperationResult ValidatePrice(int priceInStotinki)
    {
        if (priceInStotinki < 0)
        {
            return OperationResult.Failure(
                "Product price cannot be negative.");
        }

        return OperationResult.Success();
    }
}
