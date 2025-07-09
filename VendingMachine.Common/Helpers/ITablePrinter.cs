namespace VendingMachine.Common.Helpers;

public interface ITablePrinter
{
    void Print<T>(IEnumerable<T> items);
}
