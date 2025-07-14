using System.Reflection;
using VendingMachine.Common.Attributes;
using VendingMachine.Common.Constants;

namespace VendingMachine.Common.Helpers;

public class TablePrinter : ITablePrinter
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
                var formatAsCurrenCyAttribute = property.GetCustomAttribute<FormatAsCurrencyAttribute>();
                if (formatAsCurrenCyAttribute is not null && value is int intValue)
                {
                    value = string.Format(
                        VendingMachineConstants.CurrencyFormatWithDecimals, intValue / 100m);
                }

                Console.Write(
                    $"{{0,-{ColumnWidth}}}|",
                   value);
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }
}
