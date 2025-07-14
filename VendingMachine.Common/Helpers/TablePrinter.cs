using System.Reflection;
using VendingMachine.Common.Attributes;

namespace VendingMachine.Common.Helpers;

public class TablePrinter(
    ICurrencyFormatter currencyFormatter) : 
    ITablePrinter
{
    public void Print<T>(IEnumerable<T> items)
    {
        const int ColumnWidth = 25;

        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            Console.Write(@$"{{0,-{ColumnWidth}}}|", property.Name);
        }

        Console.WriteLine();
        Console.WriteLine(new string('-', properties.Length * (ColumnWidth + 1)));

        foreach (var item in items)
        {
            foreach (var property in properties)
            {
                var value = property.GetValue(item);

                var isPriceAttribute = property
                    .GetCustomAttribute<PriceAttribute>();
                var isCoinValuettribute = property
                    .GetCustomAttribute<CoinValueAttribute>();

                string? formatted = null;

                if (isPriceAttribute is not null && value is int priceInStotinki)
                {
                    formatted = currencyFormatter.FormatPrice(priceInStotinki);
                }
                else if (isCoinValuettribute is not null && value is byte coinValue)
                {
                    formatted = currencyFormatter.FormatCoinValue(coinValue);
                }

                Console.Write(
                    $"{{0,-{ColumnWidth}}}|",
                    formatted ?? value);
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }
}
