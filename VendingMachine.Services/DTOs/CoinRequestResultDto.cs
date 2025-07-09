namespace VendingMachine.Services.DTOs;

public class CoinRequestResultDto
{
    public bool IsCancelled { get; set; }
    public bool IsValid { get; set; }
    public bool IsFinished { get; set; }
    public List<byte> InsertedCoins { get; init; } = [];
}
