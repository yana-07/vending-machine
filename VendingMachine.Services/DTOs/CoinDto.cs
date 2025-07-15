using VendingMachine.Common.Attributes;

namespace VendingMachine.Services.DTOs;

public class CoinDto
{
    [SkipInTable]
    public required byte Value { get; init; }

    public required int Quantity { get; init; }

    public string Denomination =>
        Value < 100 ?
        $"{Value}st" :
        $"{Value / 100}lv";
}
