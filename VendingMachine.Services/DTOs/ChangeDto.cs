namespace VendingMachine.Services.DTOs;

public class ChangeDto
{
    public List<byte> CoinsReturned { get; init; } = [];
    public int RemainingAmount { get; set; }
}
