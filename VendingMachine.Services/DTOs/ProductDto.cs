using VendingMachine.Common.Attributes;
using VendingMachine.Data.Models;

namespace VendingMachine.Services.DTOs;

public class ProductDto
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    [Price]
    public required int Price { get; init; }

    public required byte Quantity { get; init; }
}
