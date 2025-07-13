namespace VendingMachine.Services.DTOs;

public class SellProductDto
{
    public bool IsSuccess { get; init; }

    public int ChangeToReturn { get; init; }

    public int RemainingToInsert { get; init; }
}
