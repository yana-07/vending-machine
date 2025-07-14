using VendingMachine.Common.Attributes;

namespace VendingMachine.Services.DTOs;

public class CoinDto
{
    [CoinValue]
    public required byte Value { get; init; }

    public required int Quantity { get; set; }
}
