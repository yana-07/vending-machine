namespace VendingMachine.Services.DTOs;

public class CoinDto
{
    public required byte Value { get; init; }
    public required int Quantity { get; set; }
}
