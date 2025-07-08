namespace VendingMachine.Services.DTOs;

public class SellProductDto
{
    public required bool IsSuccess { get; init; }
    public required int ChangeToReturn { get; init; }
}
