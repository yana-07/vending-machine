namespace VendingMachine.Common.Helpers;

public class TablePrinter<T> : ITablePrinter<T>
{
    public void Print(IEnumerable<T> items)
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
                Console.Write(
                    $"{{0,-{ColumnWidth}}}|",
                    property.GetValue(item));
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }
}
