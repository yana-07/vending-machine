namespace VendingMachine.Services.DTOs;

public class ChangeDto
{
    public required Dictionary<byte, int> ReturnedCoins { get; init; } = [];

    public required int RemainingChange { get; set; }
}
