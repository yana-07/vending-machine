namespace VendingMachine.Services.DTOs;

public class BuyProductDto
{
    public required bool IsSuccess { get; init; }
    public required int ChangeToReturn { get; init; }
}
