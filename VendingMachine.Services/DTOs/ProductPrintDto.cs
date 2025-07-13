namespace VendingMachine.Services.DTOs;

public class ProductPrintDto
{
    public required string Name { get; init; }

    public required string Code { get; init; }

    public required string Price { get; init; }
}