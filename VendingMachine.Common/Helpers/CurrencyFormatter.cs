using System.Globalization;
using VendingMachine.Common.Constants;

namespace VendingMachine.Common.Helpers;

public class CurrencyFormatter : ICurrencyFormatter
{
    public string FormatPrice(int priceInStotinki, string format = "F2")
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            $"{{0:{format}}}{CurrencyConstants.LevaSuffix}",
            priceInStotinki / 100m);
    }
}
