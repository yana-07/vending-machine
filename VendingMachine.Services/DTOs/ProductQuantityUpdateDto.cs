namespace VendingMachine.Services.DTOs;

public class ProductQuantityUpdateDto
{
    public required string Code { get; init; }
    public required byte NewQuantity { get; init; }
}
