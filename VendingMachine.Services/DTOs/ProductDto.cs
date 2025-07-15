using VendingMachine.Common.Attributes;
using VendingMachine.Common.Constants;

namespace VendingMachine.Services.DTOs;

public class ProductDto
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    [SkipInTable]
    public required int PriceInStotinki { get; init; }

    public required byte Quantity { get; init; }

    public string Price => 
        $"{PriceInStotinki / 100m:F2}{CurrencyConstants.LevaSuffix}";
}
