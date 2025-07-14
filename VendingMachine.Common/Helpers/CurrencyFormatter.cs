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

    public string FormatCoinValue(byte value)
    {
        if (value >= 100)
        {
            return value % 100 == 0 ?
                $"{value / 100}{CurrencyConstants.LevaSuffix}" :
                $"{value / 100m:F2}{CurrencyConstants.LevaSuffix}";
        }

        return $"{value}{CurrencyConstants.StotinkiSuffix}";
    }
}
