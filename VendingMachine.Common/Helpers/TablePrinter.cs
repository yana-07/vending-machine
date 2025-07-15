using System.Reflection;
using VendingMachine.Common.Attributes;

namespace VendingMachine.Common.Helpers;

public class TablePrinter : ITablePrinter
{
    public void Print<T>(IEnumerable<T> items)
    {
        const int ColumnWidth = 25;

        var properties = typeof(T).GetProperties()
            .Where(property => property
                .GetCustomAttribute<SkipInTableAttribute>() is null)
            .ToList();

        foreach (var property in properties)
        {
            Console.Write(@$"{{0,-{ColumnWidth}}}|", property.Name);
        }

        Console.WriteLine();
        Console.WriteLine(new string('-', properties.Count * (ColumnWidth + 1)));

        foreach (var item in items)
        {
            foreach (var property in properties)
            {
                var value = property.GetValue(item);

                Console.Write(
                    $"{{0,-{ColumnWidth}}}|",
                    value);
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }
}
