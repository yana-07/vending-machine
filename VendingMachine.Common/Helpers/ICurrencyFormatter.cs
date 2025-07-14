namespace VendingMachine.Common.Helpers;

public interface ICurrencyFormatter
{
    string FormatPrice(int priceInStotinki, string format = "F2");
    string FormatCoinValue(byte value);
}
