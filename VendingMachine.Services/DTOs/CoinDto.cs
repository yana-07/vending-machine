using VendingMachine.Common.Attributes;
using VendingMachine.Common.Constants;

namespace VendingMachine.Services.DTOs;

public class CoinDto
{
    [SkipInTable]
    public required byte Value { get; init; }

    public string Denomination =>
        Value < 100 ?
        $"{Value}{CurrencyConstants.StotinkiSuffix}" :
        $"{Value / 100}{CurrencyConstants.LevaSuffix}";

    public required int Quantity { get; init; }
}
