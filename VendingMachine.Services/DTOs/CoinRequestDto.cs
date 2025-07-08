namespace VendingMachine.Services.DTOs;

public class CoinRequestDto
{
    public bool IsCancelled { get; set; }
    public bool IsValid { get; set; }
    public bool IsFinished { get; set; }
    public List<byte> Values { get; init; } = [];
}
