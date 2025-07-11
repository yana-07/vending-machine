namespace VendingMachine.Services.DTOs;

public class ProductDto
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required int PriceInStotinki { get; init; }
    public required byte Quantity { get; init; }
}
