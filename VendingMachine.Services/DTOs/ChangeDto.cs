namespace VendingMachine.Services.DTOs;

public class ChangeDto
{
    public required List<byte> ReturnedCoins { get; init; } = [];
    public required int RemainingChange { get; set; }
}
